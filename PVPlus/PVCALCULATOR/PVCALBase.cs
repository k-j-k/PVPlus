﻿using Flee.PublicTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PVPlus.RULES;
using System.Reflection;
using System.Security.Policy;
using System.IO;

namespace PVPlus.PVCALCULATOR
{
    abstract public class PVCalculator
    {
        public ICompanyRule companyRule;
        public ProductRule productRule;
        public RiderRule riderRule;

        public LineInfo line;
        public VariableCollection variables;
        public CommutationTable c;
        public Expense ex;

        public PVCalculator stdCal; //납입기간 Min(n,20) 일 때  
        public PVCalculator stdCalofLCSV;   //저해지의 표준형

        public double 가입금액;
        public double Min_s;    //표준해약공제액 계산을 위한 가입금액

        public Dictionary<string, object> LocalVariables { get; set; }
        public string Generators { get; set; }

        public PVCalculator()
        {

        }
        public PVCalculator(LineInfo line)
        {
            this.line = line;
            variables = line.variables;

            productRule = PV.finder.FindProductRule();
            riderRule = PV.finder.FindRiderRule(line.RiderCode);
            companyRule = Configure.CompanyRule;

            ex = FindExpense();
            가입금액 = (double)variables["Amount"];
            Min_s = PV.finder.FindMin_S(riderRule.SKeyExpr.Evaluate());

            //납입기간이 m일 때와 Min(20,n)일때 기수표가 달라지는 경우(납기별 해지율 및 기납입보험료 환급 등)
            if ((int)variables["S3"] > 0)
            {
                int n = (int)variables["n"];
                int m = (int)variables["m"];
                int stdType = (int)variables["S3"];
                int Min_n = Math.Min(n, 20);
                if (m == 0) Min_n = 0;

                variables["S3"] = 0;
                variables["m"] = Min_n;

                PVCalculator stdCal = line.GetPVCalculator();

                variables["S3"] = stdType;
                variables["m"] = m;

                this.stdCal = stdCal;
            }

            //저해지의 경우 표준형(w=0) PVCal을 helper클래스에 저장
            if ((int)variables["S5"] > 0)
            {
                int csvType = (int)variables["S5"];
                variables["S5"] = 0;

                PVCalculator stdCalofLCSV = line.GetPVCalculator();
                helper.stdCalofLCSV = stdCalofLCSV;

                variables["S5"] = csvType;

                this.stdCalofLCSV = stdCalofLCSV;
            }

            c = MakeCommutationTable();

            Generators = line.GetPVGeneratorKey();
            LocalVariables = variables.ToDictionary(x => x.Key, y => y.Value);
            helper.pvCals[Generators] = this;
        }

        public virtual double Get순보험료(int n, int m, int t, int freq)
        {
            double NP = 0;
            double payCnt = Get연납입횟수(freq);
            double NNx = GetNNx(c.Nx_납입자, c.Dx_납입자, freq, 0, m);

            if (freq == 99)
            {
                NP = (c.Mx_급부[0] - c.Mx_급부[n]) / c.Dx_납입자[0];
            }
            else
            {
                NP = (c.Mx_급부[0] - c.Mx_급부[n]) / (payCnt * NNx);
            }

            return NP;
        }
        public virtual double Get기준연납순보험료(int n, int m, int t, int freq)
        {
            int Min_n = Math.Min(n, 20);

            if (stdCal == null)
            {
                return Get순보험료(n, Min_n, t, 12);
            }
            else
            {
                return stdCal.Get순보험료(n, Min_n, t, 12);
            }
        }
        public virtual double Get위험보험료(int n, int m, int t, int freq)
        {
            return Get순보험료(n, m, t, freq);
        }
        public virtual double Get저축보험료(int n, int m, int t, int freq)
        {
            return Get순보험료(n, m, t, freq) - Get위험보험료(n, m, t, freq);
        }
        public virtual double GetBeta순보험료(int n, int m, int t, int freq)
        {
            return Get순보험료(n, m, t, freq);
        }
        public virtual double Get영업보험료(int n, int m, int t, int freq)
        {
            double 분자 = 0;
            double 분모 = 1.0;

            double payCnt = Get연납입횟수(freq);
            double APV = Get연금현가(c.Nx_납입자, c.Dx_납입자, freq, 0, m);
            double NP = Get순보험료(n, m, t, freq);
            double NPSTD = Get기준연납순보험료(n, m, t, 12);

            if (freq == 99)
            {
                분자 = NP;
                분모 = 1 - ex.Alpha_P - ex.Alpha2_P - ex.Beta_P - ex.Gamma - ex.Ce;
            }
            else
            {
                분자 = NP + (ex.Alpha_S + NPSTD * ex.Alpha_Statutory) / (payCnt * APV) + (ex.Beta_S / payCnt);
                분모 = (1.0 - ex.Alpha_P / APV - ex.Alpha2_P - ex.Beta_P - ex.Gamma - ex.Ce);
            }

            return 분자 / 분모;
        }
        public virtual double Get준비금(int n, int m, int t, int freq)
        {
            double 순보험료 = Get순보험료(n, m, t, freq);
            double payCnt = Get연납입횟수(freq);
            double NNx_납입자 = GetNNx(c.Nx_납입자, c.Dx_납입자, freq, t, m);

            double 분자 = 0;
            double 분모 = 1.0;

            double 분자Out = c.Mx_급부[t] - c.Mx_급부[n];
            double 분자In = (m > 0 && t <= m) ? 순보험료 * payCnt * NNx_납입자 : 0;

            if (freq == 99)
            {
                분자 = 분자Out;
                분모 = c.Dx_유지자[t];
            }
            else
            {
                분자 = 분자Out - 분자In;
                분모 = c.Dx_유지자[t];
            }

            return 분자 / 분모;
        }

        public virtual double Get예정신계약비(int n, int m, int t, int freq)
        {
            return Eval("ALPHA_UNIT", n, m, t, freq);
        }
        public virtual double Get표준해약공제액(int n, int m, int t, int freq)
        {
            return Eval("STDALPHA_UNIT", n, m, t, freq);
        }
        public virtual double Get미상각신계약비(int n, int m, int t, int freq)
        {
            return Eval("DAC_UNIT", n, m, t, freq);
        }
        public virtual double Get해약환급금(int n, int m, int t, int freq)
        {
            return Eval("W_UNIT", n, m, t, freq);
        }

        public double Eval(string chkItem, int n, int m, int t, int freq)
        {
            if (chkItem == "NP") return Get순보험료(n, m, t, freq);
            if (chkItem == "RP") return Get위험보험료(n, m, t, freq);
            if (chkItem == "STDNP") return Get기준연납순보험료(n, m, t, freq);
            if (chkItem == "BETANP") return GetBeta순보험료(n, m, t, freq);
            if (chkItem == "GP") return Get영업보험료(n, m, t, freq);
            if (chkItem == "V") return Get준비금(n, m, t, freq);
            if (chkItem == "Min_S") return Min_s;

            helper.cal = this;

            int n_Temp = (int)variables["n"];
            int m_Temp = (int)variables["m"];
            int t_Temp = (int)variables["t"];
            int freq_Temp = (int)variables["Freq"];

            variables["n"] = n;
            variables["m"] = m;
            variables["t"] = t;
            variables["Freq"] = freq;

            IGenericExpression<double> expr = PV.finder.FindChkExprs(chkItem).산출수식;
            double val = expr.Evaluate();

            variables["n"] = n_Temp;
            variables["m"] = m_Temp;
            variables["t"] = t_Temp;
            variables["Freq"] = freq_Temp;
 
            PV.finder.FindChkExprs(chkItem).값 = val;

            return val;
        }

        public virtual Dictionary<string, double[]> FindQx(int age, int n, int m)
        {
            Dictionary<string, double[]> RateQx = new Dictionary<string, double[]>();

            //위험률 q1 ~ q30
            foreach (var s in riderRule.RateKeyByRateVariable)
            {
                string rateVariableName = s.Key;
                RateRule rateRule = PV.finder.FindRateRule(s.Value, variables);
                RateQx.Add(rateVariableName, new double[CommutationTable.MAXSIZE]);

                int rateNum = int.Parse(s.Key.Replace("q", ""));
                int t0 = (int)variables[$"t{rateNum}"];

                for (int t = 0; t <= n; t++)
                {
                    if (rateRule.기간 == 0)
                    {
                        RateQx[rateVariableName][t] = rateRule.RateArr[0 + t0];
                    }
                    else if (rateRule.기간 == 1)
                    {
                        RateQx[rateVariableName][t] = rateRule.RateArr[age + t + t0];
                    }
                    else if (rateRule.기간 == 2)
                    {
                        RateQx[rateVariableName][t] = rateRule.RateArr[t + t0];
                    }
                    else
                    {
                        throw new Exception($"{s.Value}의 기간구분이 잘 못 입력되었습니다.");
                    }
                }
            }

            return RateQx;
        }
        public virtual Expense FindExpense()
        {
            int substandardType = (int)variables["S2"];
            if (substandardType > 0) return new Expense();
            if (riderRule.담보코드 == "정기사망") return new Expense();

            else return PV.finder.FindExpense(riderRule, variables);
        }
        public virtual CommutationTable MakeCommutationTable()
        {
            CommutationTable c = new CommutationTable();
            int MAXSIZE = CommutationTable.MAXSIZE; //131
            int csvType = (int)variables["S5"];

            int age = (int)variables["Age"];
            int n = (int)variables["n"];
            int m = (int)variables["m"];

            Dictionary<string, double[]> RateQx = FindQx(age, n, m);

            for (int i = 0; i < riderRule.급부Exprs.Count(); i++)
            {
                c.RateSegments_급부.Add(new double[MAXSIZE]);
                c.RateSegments_유지자.Add(new double[MAXSIZE]);
            }

            bool r1Exist = riderRule.r1Expr.ToString() != "0";
            bool r2Exist = riderRule.r2Expr.ToString() != "0";
            bool r3Exist = riderRule.r3Expr.ToString() != "0";
            bool r4Exist = riderRule.r4Expr.ToString() != "0";
            bool r5Exist = riderRule.r5Expr.ToString() != "0";
            bool r6Exist = riderRule.r6Expr.ToString() != "0";
            bool r7Exist = riderRule.r7Expr.ToString() != "0";
            bool r8Exist = riderRule.r8Expr.ToString() != "0";
            bool r9Exist = riderRule.r9Expr.ToString() != "0";

            bool r10Exist = riderRule.r10Expr.ToString() != "0";
            bool k1Exist = riderRule.k1Expr.ToString() != "0";
            bool k2Exist = riderRule.k2Expr.ToString() != "0";
            bool k3Exist = riderRule.k3Expr.ToString() != "0";
            bool k4Exist = riderRule.k4Expr.ToString() != "0";
            bool k5Exist = riderRule.k5Expr.ToString() != "0";
            bool k6Exist = riderRule.k6Expr.ToString() != "0";
            bool k7Exist = riderRule.k7Expr.ToString() != "0";
            bool k8Exist = riderRule.k8Expr.ToString() != "0";
            bool k9Exist = riderRule.k9Expr.ToString() != "0";
            bool k10Exist = riderRule.k10Expr.ToString() != "0";

            for (int t = 0; t <= n; t++)
            {
                variables["t"] = t;
                foreach (var s in RateQx)
                {
                    variables[s.Key] = s.Value[t];
                }

                c.Rate_위험률 = RateQx;
                IDynamicExpression 이율expr = PV.finder.FindVariableChanger(productRule.상품코드, riderRule.담보코드).SingleOrDefault(x => x.변수명 == "i")?.값;
                variables["i"] = 이율expr?.Evaluate() ?? productRule.예정이율;
                variables["w"] = (csvType > 0) ? riderRule.wExpr.Evaluate() : 0;

                c.Rate_이율[t] = (double)variables["i"];
                c.Rate_해지율[t] = (double)variables["w"];

                if (r1Exist) variables["r1"] = riderRule.r1Expr.Evaluate();
                if (r2Exist) variables["r2"] = riderRule.r2Expr.Evaluate();
                if (r3Exist) variables["r3"] = riderRule.r3Expr.Evaluate();
                if (r4Exist) variables["r4"] = riderRule.r4Expr.Evaluate();
                if (r5Exist) variables["r5"] = riderRule.r5Expr.Evaluate();
                if (r6Exist) variables["r6"] = riderRule.r6Expr.Evaluate();
                if (r7Exist) variables["r7"] = riderRule.r7Expr.Evaluate();
                if (r8Exist) variables["r8"] = riderRule.r8Expr.Evaluate();
                if (r9Exist) variables["r9"] = riderRule.r9Expr.Evaluate();
                if (r10Exist) variables["r10"] = riderRule.r10Expr.Evaluate();

                if (k1Exist) variables["k1"] = riderRule.k1Expr.Evaluate();
                if (k2Exist) variables["k2"] = riderRule.k2Expr.Evaluate();
                if (k3Exist) variables["k3"] = riderRule.k3Expr.Evaluate();
                if (k4Exist) variables["k4"] = riderRule.k4Expr.Evaluate();
                if (k5Exist) variables["k5"] = riderRule.k5Expr.Evaluate();
                if (k6Exist) variables["k6"] = riderRule.k6Expr.Evaluate();
                if (k7Exist) variables["k7"] = riderRule.k7Expr.Evaluate();
                if (k8Exist) variables["k8"] = riderRule.k8Expr.Evaluate();
                if (k9Exist) variables["k9"] = riderRule.k9Expr.Evaluate();
                if (k10Exist) variables["k10"] = riderRule.k10Expr.Evaluate();

                if (r1Exist) c.Rate_r1[t] = (double)variables["r1"];
                if (r2Exist) c.Rate_r2[t] = (double)variables["r2"];
                if (r3Exist) c.Rate_r3[t] = (double)variables["r3"];
                if (r4Exist) c.Rate_r4[t] = (double)variables["r4"];
                if (r5Exist) c.Rate_r5[t] = (double)variables["r5"];
                if (r6Exist) c.Rate_r6[t] = (double)variables["r6"];
                if (r7Exist) c.Rate_r7[t] = (double)variables["r7"];
                if (r8Exist) c.Rate_r8[t] = (double)variables["r8"];
                if (r9Exist) c.Rate_r9[t] = (double)variables["r9"];
                if (r10Exist) c.Rate_r10[t] = (double)variables["r10"];

                if (k1Exist) c.Rate_k1[t] = (double)variables["k1"];
                if (k2Exist) c.Rate_k2[t] = (double)variables["k2"];
                if (k3Exist) c.Rate_k3[t] = (double)variables["k3"];
                if (k4Exist) c.Rate_k4[t] = (double)variables["k4"];
                if (k5Exist) c.Rate_k5[t] = (double)variables["k5"];
                if (k6Exist) c.Rate_k6[t] = (double)variables["k6"];
                if (k7Exist) c.Rate_k7[t] = (double)variables["k7"];
                if (k8Exist) c.Rate_k8[t] = (double)variables["k8"];
                if (k9Exist) c.Rate_k9[t] = (double)variables["k9"];
                if (k10Exist) c.Rate_k10[t] = (double)variables["k10"];

                c.Rate_유지자[t] = riderRule.유지자Expr.Evaluate();
                c.Rate_납입자[t] = riderRule.납입자Expr.Evaluate();
                c.Rate_납입자급부[t] = riderRule.납입자급부Expr.Evaluate();
                c.Rate_납입면제자급부[t] = riderRule.납입면제자급부Expr.Evaluate();

                for (int i = 0; i < riderRule.급부Exprs.Count(); i++)
                {
                    c.RateSegments_급부[i][t] = riderRule.급부Exprs[i].Evaluate();
                    c.RateSegments_유지자[i][t] = riderRule.탈퇴Exprs[i].Evaluate();
                }
            }

            c.Rate_할인율 = Enumerable.Range(0, MAXSIZE).Select(x => 1 / (1 + c.Rate_이율[x])).ToArray();
            c.Rate_할인율누계 = Enumerable.Range(0, MAXSIZE).Select(x => c.Pow(c.Rate_할인율, x)).ToArray();

            c.Lx_납입자 = c.GetLx(c.Rate_납입자);
            c.Lx_유지자 = c.GetLx(c.Rate_유지자);
            c.LxSegments_유지자 = c.RateSegments_유지자.Select(x => c.GetLx(x)).ToList();
            c.Lx_납입면제자 = Enumerable.Range(0, MAXSIZE).Select(i => c.Lx_유지자[i] - c.Lx_납입자[i]).ToArray();

            c.Dx_납입자 = c.GetDx(c.Lx_납입자);
            c.Dx_유지자 = c.GetDx(c.Lx_유지자);

            c.Nx_납입자 = c.GetNx(c.Dx_납입자);
            c.Nx_유지자 = c.GetNx(c.Dx_유지자);

            c.CxSegments_급부 = c.LxSegments_유지자.Zip(c.RateSegments_급부, (x, y) => c.GetCx(x, y)).ToList();
            c.Cx_납입자급부 = c.GetCx(c.Lx_납입자, c.Rate_납입자급부);
            c.Cx_납입면제자급부 = c.GetCx(c.Lx_납입면제자, c.Rate_납입면제자급부);

            c.MxSegments_급부 = c.CxSegments_급부.Select(x => c.GetMx(x)).ToList();
            c.MxSegments_급부합계 = Enumerable.Range(0, MAXSIZE).Select(x => c.MxSegments_급부.Sum(y => y[x])).ToArray();
            c.Mx_납입자급부 = c.GetMx(c.Cx_납입자급부);
            c.Mx_납입면제자급부 = c.GetMx(c.Cx_납입면제자급부);
            c.Mx_급부 = Enumerable.Range(0, MAXSIZE).Select(i => c.MxSegments_급부합계[i] + c.Mx_납입자급부[i] + c.Mx_납입면제자급부[i]).ToArray();

            return c;
        }

        protected double GetNNx(double[] Nx, double[] Dx, int freq, int start, int end)
        {
            double NNx = Nx[start] - Nx[end];

            switch (freq)
            {
                case 6:
                    NNx = NNx - 1.0 / 4.0 * (Dx[start] - Dx[end]);
                    break;

                case 3:
                    NNx = NNx - 3.0 / 8.0 * (Dx[start] - Dx[end]);
                    break;

                case 2:
                    NNx = NNx - 5.0 / 12.0 * (Dx[start] - Dx[end]);
                    break;

                case 1:
                    NNx = NNx - 11.0 / 24.0 * (Dx[start] - Dx[end]);
                    break;

                default:
                    break;
            }

            return Math.Max(NNx, 0);
        }
        protected double GetMMx(double[] Mx, int start, int end)
        {
            return Mx[start] - Mx[end];
        }
        protected double Get연금현가(double[] Nx, double[] Dx, int freq, int start, int end)
        {
            return GetNNx(Nx, Dx, freq, start, end) / Dx[start];
        }
        protected int Get연납입횟수(int freq)
        {
            switch (freq)
            {
                case 12:
                    return 1;
                case 6:
                    return 2;
                case 3:
                    return 4;
                case 2:
                    return 6;
                case 1:
                    return 12;
                default:
                    return 1;
            }
        }
    }
}
