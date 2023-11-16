﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flee.PublicTypes;
using System.IO;
using PVPlus.PVCALCULATOR;
using PVPlus.RULES;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace PVPlus
{
    public static class helper
    {
        #region 이전 라인정보

        public static string PreMP { get; set; }
        public static PVResult PreResult { get; set; }

        #endregion

        public static LineInfo lineInfo { get; set;}
        public static VariableCollection variables { get; set; }
        public static Dictionary<string, PVCalculator> pvCals { get; set; }

        public static bool Renewal()
        {
            if ((int)variables["S1"] > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static int AgeSign(int t)
        {
            if ((int)variables["Age"] < t)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public static double D(params double[] items)
        {
            int t = (int)variables["t"];

            if (Renewal() || t >= items.Length)
            {
                return 1.0;
            }
            else
            {
                return items[t];
            }
        }

        public static double LD(params double[] items)
        {
            int t = (int)variables["t"];
            int n = (int)variables["n"];

            if (n - t > items.Length)
            {
                return 1.0;
            }
            else if (n - t > 0)
            {
                return items[n - t - 1];
            }
            else
                return 1.0;
        }

        public static double U(params double[] items)
        {
            int t = (int)variables["t"];

            if (Renewal() || AgeSign(15) == 0 || t >= items.Length)
            {
                return 1.0;
            }
            else
            {
                return items[t];
            }
        }

        public static double S(double K)
        {
            if ((string)variables["Substandard_Mode"] == "sub")
            {
                return K;
            }
            else
            {
                return 1.0;
            }
        }

        public static bool ProductNameContains(string s)
        {
            return PV.finder.FindProductRule().상품명.Contains(s);
        }

        public static bool RiderNameContains(string s)
        {
            return PV.finder.FindRiderRule(lineInfo.RiderCode).담보명.Contains(s);
        }


        //계산
        public static PVCalculator cal { get; set; }

        public static double EVal(string chkItem)
        {
            int n = (int)variables["n"];
            int m = (int)variables["m"];
            int t = (int)variables["t"];
            int freq = (int)variables["Freq"];

            return cal.Eval(chkItem, n, m, t, freq);
        }

        public static double EVal(string chkItem, int n, int m, int t, int freq)
        {
            return cal.Eval(chkItem, n, m, t, freq);
        }

        public static double Ex(string exStr)
        {
            return (double)typeof(Expense).GetProperty(exStr).GetValue(cal.ex);
        }

        public static double Pr(int freq)
        {
            int n = (int)variables["n"];
            int m = (int)variables["m"];

            return cal.Get위험보험료(n, m, 0, freq);
        }

        public static double Pr(int age, int n, int m, int freq)
        {
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { { "Age", age }, { "n", n }, { "m", m }, { "Freq", freq } };
            PVCalculator calPr = lineInfo.GetPVCalculator(null, otherVariables);

            return calPr.Get위험보험료(n, m, 0, freq);
        }

        public static double PrTerm(int freq)
        {
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { };
            PVCalculator calTerm = lineInfo.GetPVCalculator("정기사망", otherVariables);

            int n = (int)variables["n"];
            int m = (int)variables["m"];

            return calTerm.Get위험보험료(n, m, 0, freq);
        }

        public static double PrTerm(int age, int n, int m, int freq)
        {
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { { "Age", age }, { "n", n }, { "m", m }, { "Freq", freq } };
            PVCalculator calTerm = lineInfo.GetPVCalculator("정기사망", otherVariables);

            return calTerm.Get위험보험료(n, m, 0, freq);
        }


        //위험률
        public static double FindQ(string key, int offset)
        {
            return PV.finder.FindRateRule(key, variables).RateArr[offset];
        }

        public static double FindQ(int index, int offset)
        {
            //위험률 q1 ~ q30
            string rateVariableName = $"q{index}";
            string rateKey = PV.finder.FindRiderRule(lineInfo.RiderCode).RateKeyByRateVariable[rateVariableName];
            RateRule rateRule = PV.finder.FindRateRule(rateKey, variables);

            return rateRule.RateArr[offset];
        }


        //산출 값(다른 담보의 값)
        public static double Ax(string riderCode, int age, int n)
        {
            //일시납 보험료
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { { "Age", age }, { "n", n }, { "m", 0 }, { "Freq", 99 }, { "S3", 0 }, {"S5", 0 } };
            PVCalculator cal = lineInfo.GetPVCalculator(riderCode, otherVariables);

            return cal.Get순보험료(n, 0, 0, 99);
        }

        public static double GP(string riderCode, int age, int n, int m, int freq)
        {
            PVCalculator cal = lineInfo.GetPVCalculator(riderCode, new Dictionary<string, object>());

            return cal.Get영업보험료(n, m, 0, freq);

        }

        public static double Xx(string riderCode, string type, int age, int n, int m, int t)
        {
            //위험보험료
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { { "Age", age }, { "n", n }, { "m", m } };
            PVCalculator cal = lineInfo.GetPVCalculator(riderCode, otherVariables);

            if (type == "Dx_유지자") return cal.c.Dx_유지자[t];
            if (type == "Dx_납입자") return cal.c.Dx_납입자[t];
            if (type == "Mx_급부") return cal.c.Mx_급부[t];
            if (type == "Rx_급부") return cal.c.Mx_급부.Skip(t).Sum();
            //... 필요 시 추가

            return 0;
        }

        public static double V(string riderCode, int age, int n, int m, int freq, int t)
        {
            //준비금
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { { "Age", age }, { "n", n }, { "m", m } };
            PVCalculator cal = lineInfo.GetPVCalculator(riderCode, otherVariables);

            return cal.Get준비금(n, m, t, freq);
        }

        public static double Pr(string riderCode, int age, int n, int m, int freq)
        {
            //위험보험료
            Dictionary<string, object> otherVariables = new Dictionary<string, object>() { { "Age", age }, { "n", n }, { "m", m } };
            PVCalculator cal = lineInfo.GetPVCalculator(riderCode, otherVariables);

            return cal.Get위험보험료(n, m, 0, freq);
        }



        //저해지의 표준형 산출 값
        public static PVCalculator stdCalofLCSV { get; set; }

        public static double V(int t)
        {
            if (stdCalofLCSV == null || (int)variables["S5"] == 0) return 0;

            int n = (int)variables["n"];
            int m = (int)variables["m"];
            int freq = (int)variables["Freq"];
            double V = stdCalofLCSV.Eval("V2_UNIT", n, m, t, freq);
            return V;
        }

        public static double W(int t)
        {
            if (stdCalofLCSV == null || (int)variables["S5"] == 0) return 0;

            int n = (int)variables["n"];
            int m = (int)variables["m"];
            int freq = (int)variables["Freq"];
            double W = stdCalofLCSV.Eval("W_UNIT", n, m, t, freq);
            return W;
        }

        public static double V(int t, int ac)
        {
            if (stdCalofLCSV == null || (int)variables["S5"] == 0) return 0;

            int n = (int)variables["n"];
            int m = (int)variables["m"];
            int freq = (int)variables["Freq"];
            int S6 = (int)variables["S6"];

            variables["S6"] = ac;
            double V = stdCalofLCSV.Eval("V2_UNIT", n, m, t, freq);
            variables["S6"] = S6;

            return V;
        }

        public static double W(int t, int ac)
        {
            if (stdCalofLCSV == null || (int)variables["S5"] == 0) return 0;

            int n = (int)variables["n"];
            int m = (int)variables["m"];
            int freq = (int)variables["Freq"];
            int S6 = (int)variables["S6"];

            variables["S6"] = ac;
            double W = stdCalofLCSV.Eval("W_UNIT", n, m, t, freq);
            variables["S6"] = S6;

            return W;
        }

        #region Round, Min/Max

        public static double RoundUp(double number, int digt)
        {
            double n = Math.Pow(10.0, digt);
            return Math.Ceiling(number * n) / n;
        }

        public static double RoundDown(double number, int digt)
        {
            double n = Math.Pow(10.0, digt);
            return Math.Floor(number * n) / n;
        }

        public static double Round2(double number, int digt)
        {
            double n;

            n = Math.Round(number, 10, MidpointRounding.AwayFromZero); //머신에러 방지용
            if (n == number) n = Math.Round(number, 9);
            n = Math.Round(n, digt, MidpointRounding.AwayFromZero);

            return n;
        }

        public static double Round2(double number)
        {
            return Round2(number, 0);
        }

        public static double RoundA(double number)
        {
            double Amount = (double)variables["Amount"];

            return Round2(number * Amount, 0) / Amount;
        }

        public static double PositiveMin(params double[] vals)
        {
            double[] val2 = vals.Where(x => x > 0).ToArray();
            if (val2.Any()) return val2.Min();
            else return 0;
        }

        public static double PositiveMax(params double[] vals)
        {
            double[] val2 = vals.Where(x => x > 0).ToArray();
            if (val2.Any()) return val2.Max();
            else return 0;
        }

        public static double Average(params double[] vals)
        {
            return vals.Average();
        }

        public static string TypeOf(object s)
        {
            try
            {
                return s.GetType().ToString();
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        #region Ifs/Choose/Convert...
        public static int Choose(int index, params int[] items)
        {
            int idx = Math.Max(Math.Min(index, items.Length), 1);
            return items[idx - 1];
        }
        public static double Choose(int index, params double[] items)
        {
            int idx = Math.Max(Math.Min(index, items.Length), 1);
            return items[idx - 1];
        }
        public static string Choose(int index, params string[] items)
        {
            int idx = Math.Max(Math.Min(index, items.Length), 1);
            return items[idx - 1];
        }

        public static int ToInt(object s)
        {
            return Convert.ToInt32(s);
        }
        public static double ToDouble(object s)
        {
            return Convert.ToDouble(s);
        }
        public static string ToString(object s)
        {
            return s.ToString();
        }

        public static int ToInt(object item, string items)
        {
            List<string[]> list = items.Split(',').Select(x => x.Split(new string[] { "->" }, StringSplitOptions.None)).ToList();

            for (int i = 0; i < list.Count(); i++)
            {
                if (list[i][0].ToString().Trim() == item.ToString().Trim()) return Convert.ToInt32(list[i][1]);
            }

            throw new Exception($"ToInt({item}, {items}) 함수의 계산과정에서 오류가 발생 하였습니다..");
        }
        public static double ToDouble(object item, string items)
        {
            List<string[]> list = items.Split(',').Select(x => x.Split(new string[] { "->" }, StringSplitOptions.None)).ToList();

            for (int i = 0; i < list.Count(); i++)
            {
                if (list[i][0].ToString().Trim() == item.ToString().Trim()) return Convert.ToDouble(list[i][1]);
            }

            throw new Exception($"ToDouble({item}, {items}) 함수의 계산과정에서 오류가 발생 하였습니다..");
        }
        public static string ToString(object item, string items)
        {
            List<string[]> list = items.Split(',').Select(x => x.Split(new string[] { "->" }, StringSplitOptions.None)).ToList();

            for (int i = 0; i < list.Count(); i++)
            {
                if (list[i][0].ToString().Trim() == item.ToString().Trim()) return Convert.ToString(list[i][1]);
            }

            throw new Exception($"ToString({item}, {items}) 함수의 계산과정에서 오류가 발생 하였습니다..");
        }

        public static int IndexOf(object item, params object[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].ToString() == item.ToString()) return i + 1;
            }

            throw new Exception($"IndexOf({item}, {string.Join(",", items)}) 함수의 인덱스를 찾을 수 없습니다.");
        }

        public static double Ifs(Boolean condition1, double val1, double dafaultVal)
        {
            if (condition1) return val1;
            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, Boolean condition5, double val5, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, Boolean condition5, double val5, Boolean condition6, double val6, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;

            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, Boolean condition5, double val5, Boolean condition6, double val6, Boolean condition7, double val7, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;

            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, Boolean condition5, double val5, Boolean condition6, double val6, Boolean condition7, double val7, Boolean condition8, double val8, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;

            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, Boolean condition5, double val5, Boolean condition6, double val6, Boolean condition7, double val7, Boolean condition8, double val8, Boolean condition9, double val9, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;
            if (condition9) return val9;

            return dafaultVal;
        }
        public static double Ifs(Boolean condition1, double val1, Boolean condition2, double val2, Boolean condition3, double val3, Boolean condition4, double val4, Boolean condition5, double val5, Boolean condition6, double val6, Boolean condition7, double val7, Boolean condition8, double val8, Boolean condition9, double val9, Boolean condition10, double val10, double dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;
            if (condition9) return val9;
            if (condition10) return val10;

            return dafaultVal;
        }

        public static int Ifs(Boolean condition1, int val1, int dafaultVal)
        {
            if (condition1) return val1;
            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, Boolean condition5, int val5, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, Boolean condition5, int val5, Boolean condition6, int val6, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;

            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, Boolean condition5, int val5, Boolean condition6, int val6, Boolean condition7, int val7, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;

            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, Boolean condition5, int val5, Boolean condition6, int val6, Boolean condition7, int val7, Boolean condition8, int val8, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;

            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, Boolean condition5, int val5, Boolean condition6, int val6, Boolean condition7, int val7, Boolean condition8, int val8, Boolean condition9, int val9, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;
            if (condition9) return val9;

            return dafaultVal;
        }
        public static int Ifs(Boolean condition1, int val1, Boolean condition2, int val2, Boolean condition3, int val3, Boolean condition4, int val4, Boolean condition5, int val5, Boolean condition6, int val6, Boolean condition7, int val7, Boolean condition8, int val8, Boolean condition9, int val9, Boolean condition10, int val10, int dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;
            if (condition9) return val9;
            if (condition10) return val10;


            return dafaultVal;
        }

        public static string Ifs(Boolean condition1, string val1, string dafaultVal)
        {
            if (condition1) return val1;
            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, Boolean condition5, string val5, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, Boolean condition5, string val5, Boolean condition6, string val6, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;

            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, Boolean condition5, string val5, Boolean condition6, string val6, Boolean condition7, string val7, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;

            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, Boolean condition5, string val5, Boolean condition6, string val6, Boolean condition7, string val7, Boolean condition8, string val8, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;

            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, Boolean condition5, string val5, Boolean condition6, string val6, Boolean condition7, string val7, Boolean condition8, string val8, Boolean condition9, string val9, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;
            if (condition9) return val9;

            return dafaultVal;
        }
        public static string Ifs(Boolean condition1, string val1, Boolean condition2, string val2, Boolean condition3, string val3, Boolean condition4, string val4, Boolean condition5, string val5, Boolean condition6, string val6, Boolean condition7, string val7, Boolean condition8, string val8, Boolean condition9, string val9, Boolean condition10, string val10, string dafaultVal)
        {
            if (condition1) return val1;
            if (condition2) return val2;
            if (condition3) return val3;
            if (condition4) return val4;
            if (condition5) return val5;
            if (condition6) return val6;
            if (condition7) return val7;
            if (condition8) return val8;
            if (condition9) return val9;
            if (condition10) return val10;


            return dafaultVal;
        }

        #endregion
    }
}
