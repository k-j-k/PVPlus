using Flee.PublicTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVPlus.RULES
{
    public class DataReader
    {
        public ExpressionContext Context { get; private set; }
        public string ProductCode { get; set; }
        public TableType TableType { get; set; }

        public DataReader(string productCode, TableType tableType)
        {
            Context = new ExpressionContext();
            Context.Imports.AddType(typeof(Math));
            Context.Imports.AddType(typeof(helper));

            //helper클래스 초기화
            typeof(helper).GetProperties()
                .Where(x => x.MemberType == System.Reflection.MemberTypes.Property || x.MemberType == System.Reflection.MemberTypes.Field)
                .ToList().ForEach(x => x.SetValue(this, x.GetType().IsValueType ? Activator.CreateInstance(x.GetType()) : null));

            ProductCode = productCode;
            TableType = tableType;

            productRuleFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "Product.txt"));
            riderRuleFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "Rider.txt"));
            pLayoutFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "LayoutP.txt"));
            vLayoutFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "LayoutV.txt"));
            sLayoutFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "LayoutS.txt"));
            varChgFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "VarChg.txt"));
            rateFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "Rate.txt"));
            expenseFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "Expense.txt"));
            sFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "SInfo.txt"));
            evaluatedSFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "EvaluatedSInfo.txt"));
            chkExprsFI = new FileInfo(Path.Combine(Configure.WorkingDI.FullName, "ChkExprs.txt"));

            InitializeAllVariables();
        }

        public ProductRule ReadProductRule()
        {
            List<string[]> data = ToArrList(productRuleFI.FullName);

            string[] arr = data
                .FirstOrDefault(x => x[0] == ProductCode);

            if (arr == null) throw new Exception($"ProductRule시트에서 상품코드({ProductCode})를 찾을 수 없습니다.");

            ProductRule rule = ToProductRule(arr);
            return rule;
        }
        public List<RiderRule> ReadRiderRules()
        {
            List<string[]> data = ToArrList(riderRuleFI.FullName);

            List<RiderRule> riderRules = data
                .Where(x => x[0] == ProductCode)
                .Select(x => ToRiderRule(x))
                .ToList();


            //정기사망 기본추가
            string[] arr = Enumerable.Range(0, 110).Select(x => "").ToArray();
            RiderRule rule = ToRiderRule(arr);
            rule.상품코드 = ProductCode;
            rule.담보코드 = "정기사망";
            rule.담보명 = "정기사망";
            rule.PV_Type = CompileInt("1");
            rule.가입금액Expr = CompileDouble("1");
            rule.납입자Expr = CompileDouble("1-q1");
            rule.유지자Expr = CompileDouble("1-q1");
            rule.급부Exprs = new List<IGenericExpression<double>>() { CompileDouble("q1") };
            rule.탈퇴Exprs = new List<IGenericExpression<double>>() { CompileDouble("1-q1") };
            rule.RateKeyByRateVariable = new Dictionary<string, string>() { { "q1", "정기사망|100000000|1|000000" } };
            riderRules.Add(rule);
            //

            return riderRules;
        }
        public List<ExpenseRule> ReadExpenses()
        {
            List<string[]> data = ToArrList(expenseFI.FullName);

            List<ExpenseRule> expExprs = data
                .Where(x => x[0] == ProductCode)
                .Select(x => ToExpenseExpression(x))
                .ToList();

            if (!expExprs.Any()) throw new Exception($"사업비를 한 줄 이상 입력해 주세요({ProductCode})");

            return expExprs;
        }
        public List<RateRule> ReadRateRules()
        {
            List<string[]> data = ToArrList(rateFI.FullName);

            List<RateRule> result = data
                .Select(x => ToRateRule(x))
                .ToList();

            return result;
        }
        public List<Layout> ReadLayouts()
        {
            List<string[]> data = new List<string[]>();

            if (TableType == TableType.P) data = ToArrList(pLayoutFI.FullName);
            if (TableType == TableType.V) data = ToArrList(vLayoutFI.FullName);
            if (TableType == TableType.StdAlpha) data = ToArrList(sLayoutFI.FullName);

            List<Layout> result = data
                .Where(x => x[0] == "RiderCode" || x[0] == "Check" || x[0] == "Base" || x[0] == ProductCode)
                .Where(x => x[5] != "")
                .Where(x => !(Configure.SeperationType == "1" && x[4] == ""))
                .Where(x => !(Configure.SeperationType == "2" && x[2] == ""))
                .Select(x => ToLayout(x))
                .ToList();

            return result;
        }
        public List<VarChg> ReadVariableChanger()
        {
            List<string[]> data = ToArrList(varChgFI.FullName);

            List<VarChg> result = data
                .Where(x => x[0] == ProductCode || x[0] == "Base")
                .Select(x => ToVariableChanger(x))
                .ToList();

            return result;
        }
        public List<SInfo> ReadSInfos()
        {
            List<string[]> data = ToArrList(sFI.FullName);

            List<SInfo> result = data
                .Where(x => x[1] == ProductCode)
                .Select(x => ToSInfo(x))
                .ToList();

            return result;
        }
        public List<SInfo> ReadEvaluatedSInfos()
        {
            if (evaluatedSFI.Exists)
            {
                List<string[]> data = ToArrList(evaluatedSFI.FullName);

                List<SInfo> result = data
                    .Where(x => x[1] == ProductCode)
                    .Select(x => ToEvaluatedSInfo(x))
                    .ToList();

                return result;
            }
            else
            {
                return new List<SInfo>();
            }
        }
        public List<ChkExprs> ReadChkExprs()
        {
            List<string[]> data = ToArrList(chkExprsFI.FullName);

            List<ChkExprs> chkExprs = data
                .Select(x => ToChkExprs(x))
                .ToList();

            return chkExprs;
        }

        public void InitializeAllVariables()
        {
            InitializeFactorVariables();
            InitializeRateVariables();
            InitializeRuleVariables();
            InitializeCheckVariables();
        }
        public void InitializeFactorVariables()
        {
            //위험률 Factors
            Context.Variables["F1"] = 0;
            Context.Variables["F2"] = 0;
            Context.Variables["F3"] = 0;
            Context.Variables["F4"] = 0;
            Context.Variables["F5"] = 0;
            Context.Variables["F6"] = 0;
            Context.Variables["F7"] = 0;
            Context.Variables["F8"] = 0;
            Context.Variables["F9"] = 0;
            Context.Variables["F10"] = 0;

            //계산옵션 Factors
            Context.Variables["S1"] = 0;
            Context.Variables["S2"] = 0;
            Context.Variables["S3"] = 0;
            Context.Variables["S4"] = 0;
            Context.Variables["S5"] = 0;
            Context.Variables["S6"] = 0;
            Context.Variables["S7"] = 0;
            Context.Variables["S8"] = 0;
            Context.Variables["S9"] = 0;
            Context.Variables["S10"] = 0;

            //표준하체 할증 값 적용변수
            //norm:표준체, sub:할증체
            Context.Variables["Substandard_Mode"] = "None";

            //MP Factors
            Context.Variables["n"] = 0;
            Context.Variables["m"] = 0;
            Context.Variables["nAge"] = 0;
            Context.Variables["mAge"] = 0;
            Context.Variables["Age"] = 0;
            Context.Variables["Freq"] = 0;
            Context.Variables["Jong"] = 0;
            Context.Variables["ElapseYear"] = 0;

            Context.Variables["RiderCode"] = "";
            Context.Variables["Company"] = "";
            Context.Variables["Channel"] = 0;
            Context.Variables["PV_Type"] = 0;
            Context.Variables["S_Type"] = 0;

            Context.Variables["TempStr1"] = "";
            Context.Variables["TempStr2"] = "";
        }
        public void InitializeRateVariables()
        {
            for (int i = 1; i <= 30; i++)
            {
                Context.Variables["q" + i] = 0.0;
                Context.Variables["t" + i] = 0;
            }
            Context.Variables["w"] = 0.0;
            Context.Variables["r1"] = 0.0;
            Context.Variables["r2"] = 0.0;
            Context.Variables["r3"] = 0.0;
            Context.Variables["r4"] = 0.0;
            Context.Variables["r5"] = 0.0;
            Context.Variables["r6"] = 0.0;
            Context.Variables["r7"] = 0.0;
            Context.Variables["r8"] = 0.0;
            Context.Variables["r9"] = 0.0;
            Context.Variables["r10"] = 0.0;
            Context.Variables["k1"] = 0.0;
            Context.Variables["k2"] = 0.0;
            Context.Variables["k3"] = 0.0;
            Context.Variables["k4"] = 0.0;
            Context.Variables["k5"] = 0.0;
            Context.Variables["k6"] = 0.0;
            Context.Variables["k7"] = 0.0;
            Context.Variables["k8"] = 0.0;
            Context.Variables["k9"] = 0.0;
            Context.Variables["k10"] = 0.0;
        }
        public void InitializeRuleVariables()
        {
            Context.Variables["i"] = 0.0;
            Context.Variables["v"] = 0.0;

            Context.Variables["ii"] = 0.0;
            Context.Variables["vv"] = 0.0;

            Context.Variables["t"] = 0;
            Context.Variables["Amount"] = 0.0;
            Context.Variables["Min_S"] = 0.0;
        }
        public void InitializeCheckVariables()
        {
            Context.Variables["NP0"] = 0.0;
            Context.Variables["NP1"] = 0.0;
            Context.Variables["NP2"] = 0.0;
            Context.Variables["NP3"] = 0.0;
            Context.Variables["NP4"] = 0.0;
            Context.Variables["NP5"] = 0.0;
            Context.Variables["NP6"] = 0.0;

            Context.Variables["BETANP0"] = 0.0;
            Context.Variables["BETANP1"] = 0.0;
            Context.Variables["BETANP2"] = 0.0;
            Context.Variables["BETANP3"] = 0.0;
            Context.Variables["BETANP4"] = 0.0;
            Context.Variables["BETANP5"] = 0.0;
            Context.Variables["BETANP6"] = 0.0;

            Context.Variables["STDNP"] = 0.0;
            Context.Variables["AP"] = 0.0;

            Context.Variables["GP0"] = 0.0;
            Context.Variables["GP1"] = 0.0;
            Context.Variables["GP2"] = 0.0;
            Context.Variables["GP3"] = 0.0;
            Context.Variables["GP4"] = 0.0;
            Context.Variables["GP5"] = 0.0;
            Context.Variables["GP6"] = 0.0;

            Context.Variables["DCGP0"] = 0.0;
            Context.Variables["DCGP1"] = 0.0;
            Context.Variables["DCGP2"] = 0.0;
            Context.Variables["DCGP3"] = 0.0;
            Context.Variables["DCGP4"] = 0.0;
            Context.Variables["DCGP5"] = 0.0;
            Context.Variables["DCGP6"] = 0.0;

            Context.Variables["V0"] = 0.0;
            Context.Variables["V1"] = 0.0;
            Context.Variables["VWhole"] = new double[131];

            Context.Variables["W0"] = 0.0;
            Context.Variables["W1"] = 0.0;
            Context.Variables["WWhole"] = new double[131];

            Context.Variables["STDALPHA"] = 0.0;
            Context.Variables["ALPHA"] = 0.0;

            Context.Variables["TempCK0"] = 0.0;
            Context.Variables["TempCK1"] = 0.0;
            Context.Variables["TempCK2"] = 0.0;
            Context.Variables["TempCK3"] = 0.0;
            Context.Variables["TempCK4"] = 0.0;
            Context.Variables["TempCK5"] = 0.0;
            Context.Variables["TempCK6"] = 0.0;
            Context.Variables["TempCKA"] = 0.0;
            Context.Variables["TempCKB"] = 0.0;
            Context.Variables["TempCKC"] = 0.0;
        }

        public void GenerateEvaluatedSInfosText(List<SInfo> sList)
        {
            using (StreamWriter sw = new StreamWriter(evaluatedSFI.FullName, false, Encoding.UTF8))
            {
                sList.ForEach(x => sw.WriteLine(x.ToString()));
            }
        }

        public IGenericExpression<int> CompileInt(string exprStr)
        {
            if (string.IsNullOrWhiteSpace(exprStr)) exprStr = "0";

            try
            {
                return Context.CompileGeneric<int>(exprStr);
            }
            catch
            {
                throw new Exception("적용 할 수 없는 수식 발견:" + exprStr);
            }
        }
        public IGenericExpression<double> CompileDouble(string exprStr)
        {
            if (string.IsNullOrWhiteSpace(exprStr)) exprStr = "0";
            if (exprStr == "∞" || exprStr == "NaN") exprStr = "0";

            try
            {
                return Context.CompileGeneric<double>(exprStr);
            }
            catch
            {
                throw new Exception("적용 할 수 없는 수식 발견:" + exprStr);
            }
        }
        public IGenericExpression<string> CompileString(string exprStr)
        {
            if (string.IsNullOrWhiteSpace(exprStr)) exprStr = @""""""; //""
            try
            {
                return Context.CompileGeneric<string>(exprStr);
            }
            catch
            {
                throw new Exception("적용 할 수 없는 수식  발견:" + exprStr);
            }

        }
        public IGenericExpression<bool> CompileBool(string exprStr)
        {
            if (string.IsNullOrWhiteSpace(exprStr)) exprStr = "true";
            try
            {
                return Context.CompileGeneric<bool>(exprStr);
            }
            catch
            {
                throw new Exception("적용 할 수 없는 수식  발견:" + exprStr);
            }
        }

        public double ToDoubleOrDefault(string s, double defaultVal)
        {
            double val = 0;
            bool isDouble = double.TryParse(s, out val);

            return (isDouble) ? val : defaultVal;
        }
        public int ToIntOrDefault(string s, int defaultVal)
        {
            int val = 0;
            bool isInt = int.TryParse(s, out val);

            return (isInt) ? val : defaultVal;
        }

        private FileInfo productRuleFI;
        private FileInfo riderRuleFI;
        private FileInfo pLayoutFI, vLayoutFI, sLayoutFI;
        private FileInfo expenseFI;
        private FileInfo rateFI;
        private FileInfo sFI;
        private FileInfo varChgFI;
        private FileInfo evaluatedSFI;
        private FileInfo chkExprsFI;

        private List<string[]> ToArrList(string path)
        {
            List<string[]> result = new List<string[]>();

            using (StreamReader sr = new StreamReader(path, Encoding.Default))
            {
                while (!sr.EndOfStream)
                {
                    result.Add(sr.ReadLine().Split('\t'));
                }
            }

            return result;
        }
        private ProductRule ToProductRule(string[] arr)
        {
            ProductRule pr = new ProductRule();

            try
            {
                pr.상품코드 = arr[0];
                pr.판매시기 = arr[1];
                pr.상품명 = arr[2];
                pr.예정이율 = double.Parse(arr[3]);
                pr.평균공시이율 = ToDoubleOrDefault(arr[4], 0);
                pr.판매채널 = ToIntOrDefault(arr[5], 0);
            }
            catch
            {
                throw new Exception($"ProductRule 입력 형식이 잘 못 되었습니다. 상품코드: {arr[0]}");
            }

            return pr;
        }
        private RiderRule ToRiderRule(string[] arr)
        {
            RiderRule r = new RiderRule();

            r.상품코드 = arr[0];
            r.담보코드 = arr[1];
            r.담보명 = arr[2];
            r.PV_Type = CompileInt(arr[3]);
            r.가입금액Expr = CompileDouble(arr[4]);
            r.납입자Expr = CompileDouble(arr[5]);
            r.유지자Expr = CompileDouble(arr[6]);

            r.급부Exprs = new List<IGenericExpression<double>>();
            r.탈퇴Exprs = new List<IGenericExpression<double>>();

            for (int i = 0; i < 20; i++)
            {
                if (!string.IsNullOrWhiteSpace(arr[7 + i]))
                {
                    r.급부Exprs.Add(CompileDouble(arr[7 + i]));

                    if (!string.IsNullOrWhiteSpace(arr[27 + i]))
                    {
                        r.탈퇴Exprs.Add(CompileDouble(arr[27 + i]));
                    }
                    else
                    {
                        r.탈퇴Exprs.Add(CompileDouble(arr[6]));
                    }
                }
            }

            r.납입자급부Expr = CompileDouble(arr[47]);
            r.납입면제자급부Expr = CompileDouble(arr[48]);

            r.RateKeyByRateVariable = new Dictionary<string, string>();

            List<string> rateVariableList = new List<string>()
            {
                "q1","q2","q3","q4","q5","q6","q7","q8","q9","q10",
                "q11","q12","q13","q14","q15","q16","q17","q18","q19","q20",
                "q21","q22","q23","q24","q25","q26","q27","q28","q29","q30"
            };

            for (int i = 0; i < rateVariableList.Count(); i++)
            {
                string rateVariable = rateVariableList[i];
                string rateKey = arr[49 + i];

                if (rateKey == "")
                {
                    continue;
                }
                else
                {
                    r.RateKeyByRateVariable[rateVariable] = rateKey;
                }
            }

            r.wExpr = CompileDouble(arr[79]);

            r.r1Expr = CompileDouble(arr[80]);
            r.r2Expr = CompileDouble(arr[81]);
            r.r3Expr = CompileDouble(arr[82]);
            r.r4Expr = CompileDouble(arr[83]);
            r.r5Expr = CompileDouble(arr[84]);
            r.r6Expr = CompileDouble(arr[85]);
            r.r7Expr = CompileDouble(arr[86]);
            r.r8Expr = CompileDouble(arr[87]);
            r.r9Expr = CompileDouble(arr[88]);
            r.r10Expr = CompileDouble(arr[89]);

            r.k1Expr = CompileDouble(arr[90]);
            r.k2Expr = CompileDouble(arr[91]);
            r.k3Expr = CompileDouble(arr[92]);
            r.k4Expr = CompileDouble(arr[93]);
            r.k5Expr = CompileDouble(arr[94]);
            r.k6Expr = CompileDouble(arr[95]);
            r.k7Expr = CompileDouble(arr[96]);
            r.k8Expr = CompileDouble(arr[97]);
            r.k9Expr = CompileDouble(arr[98]);
            r.k10Expr = CompileDouble(arr[99]);

            r.S_Type = CompileInt(arr[100]);
            r.SKeyExpr = CompileString(arr[101]);

            return r;
        }
        private RateRule ToRateRule(string[] arr)
        {
            RateRule r = new RateRule();

            r.위험률Key = arr[0];
            r.위험률형태 = arr[1];
            r.위험률명 = arr[2];
            r.적용년월 = arr[3];

            r.기간 = ToIntOrDefault(arr[4], 0);
            r.성별 = ToIntOrDefault(arr[5], 0);
            r.급수 = ToIntOrDefault(arr[6], 0);
            r.운전 = ToIntOrDefault(arr[7], 0);
            r.금액 = ToIntOrDefault(arr[8], 0);
            r.사고연령 = ToIntOrDefault(arr[9], 0);
            r.가변1 = ToIntOrDefault(arr[10], 0);
            r.가변2 = ToIntOrDefault(arr[11], 0);
            r.가변3 = ToIntOrDefault(arr[12], 0);
            r.가변4 = ToIntOrDefault(arr[13], 0);

            r.RateArr = new double[131];

            for (int i = 0; i <= 130; i++)
            {
                r.RateArr[i] = ToDoubleOrDefault(arr[14 + i], 0);
            }

            return r;
        }
        private ExpenseRule ToExpenseExpression(string[] arr)
        {
            ExpenseRule exExpr = new ExpenseRule();

            exExpr.상품코드 = arr[0];
            exExpr.담보코드 = arr[1];
            exExpr.조건1 = CompileBool(arr[2]);
            exExpr.조건2 = CompileBool(arr[3]);
            exExpr.조건3 = CompileBool(arr[4]);
            exExpr.조건4 = CompileBool(arr[5]);

            exExpr.Alpha_P_Expr = CompileDouble(arr[6]);
            exExpr.Alpha2_P_Expr = CompileDouble(arr[7]);
            exExpr.Alpha_S_Expr = CompileDouble(arr[8]);
            exExpr.Alpha_Statutory_Expr = CompileDouble(arr[9]);
            exExpr.Beta_P_Expr = CompileDouble(arr[10]);
            exExpr.Beta_S_Expr = CompileDouble(arr[11]);
            exExpr.Betaprime_P_Expr = CompileDouble(arr[12]);
            exExpr.Betaprime_S_Expr = CompileDouble(arr[13]);
            exExpr.Gamma_Expr = CompileDouble(arr[14]);
            exExpr.Ce_Expr = CompileDouble(arr[15]);
            exExpr.Refund_Rate_S_Expr = CompileDouble(arr[16]);
            exExpr.Refund_Rate_P_Expr = CompileDouble(arr[17]);
            exExpr.가변1_Expr = CompileDouble(arr[18]);
            exExpr.가변2_Expr = CompileDouble(arr[19]);

            return exExpr;
        }
        private Layout ToLayout(string[] arr)
        {
            Layout layout = new Layout();

            layout.상품코드 = arr[0];
            layout.담보코드 = arr[1];
            layout.Start = ToIntOrDefault(arr[2], 0);
            layout.Length = ToIntOrDefault(arr[3], 0);
            layout.Index = ToIntOrDefault(arr[4], 0);
            layout.FactorName = arr[5];

            if (!Context.Variables.ContainsKey(layout.FactorName))
            {
                throw new Exception("정의되지 않은 LayoutFactor - " + layout.FactorName);
            }

            return layout;
        }
        private VarChg ToVariableChanger(string[] arr)
        {
            VarChg vc = new VarChg();

            try
            {
                vc.상품코드 = arr[0];
                vc.담보코드 = arr[1];
                vc.변수명 = arr[2];
                vc.값 = Context.CompileDynamic(arr[3]);
            }
            catch
            {
                throw new Exception($"VarChg의 {arr[3]}의 값이 잘 못 입력되었습니다.");
            }

            return vc;
        }
        private SInfo ToSInfo(string[] arr)
        {
            SInfo s = new SInfo();

            s.SKey = arr[0].Replace(@"""", "");
            s.상품코드 = arr[1];
            s.담보코드 = arr[2];
            s.GroupKey1 = ToIntOrDefault(arr[3], 0);
            s.GroupKey2 = ToIntOrDefault(arr[4], 0);
            s.GroupKey3 = ToIntOrDefault(arr[5], 0);

            s.x = ToIntOrDefault(arr[6], 40);
            s.n = ToIntOrDefault(arr[7], 3);
            s.m = ToIntOrDefault(arr[8], 3);
            s.성별 = ToIntOrDefault(arr[9], 1);
            s.급수 = ToIntOrDefault(arr[10], 1);
            s.운전 = ToIntOrDefault(arr[11], 1);
            s.금액 = ToIntOrDefault(arr[12], 1);
            s.사고연령 = ToIntOrDefault(arr[13], s.x);
            s.가변1 = ToIntOrDefault(arr[14], 1);
            s.가변2 = ToIntOrDefault(arr[15], 1);
            s.가변3 = ToIntOrDefault(arr[16], 1);
            s.가변4 = ToIntOrDefault(arr[17], 1);

            s.VarAdd = arr[18];
            s.위험보험료Expr = CompileDouble(arr[19]);
            s.정기위험보험료Expr = CompileDouble(arr[20]);
            s.SExpr = CompileDouble(arr[21]);
            s.Min_S = ToDoubleOrDefault(arr[22], 0);
            s.ErrorMessage = arr[23];

            return s;
        }
        private SInfo ToEvaluatedSInfo(string[] arr)
        {
            SInfo s = new SInfo();

            s.SKey = arr[0].Replace(@"""", "");
            s.상품코드 = arr[1];
            s.담보코드 = arr[2];
            s.GroupKey1 = ToIntOrDefault(arr[3], 0);
            s.GroupKey2 = ToIntOrDefault(arr[4], 0);
            s.GroupKey3 = ToIntOrDefault(arr[5], 0);

            s.x = ToIntOrDefault(arr[6], 40);
            s.n = ToIntOrDefault(arr[7], 3);
            s.m = ToIntOrDefault(arr[8], 3);
            s.성별 = ToIntOrDefault(arr[9], 1);
            s.급수 = ToIntOrDefault(arr[10], 1);
            s.운전 = ToIntOrDefault(arr[11], 1);
            s.금액 = ToIntOrDefault(arr[12], 1);
            s.사고연령 = ToIntOrDefault(arr[13], s.x);
            s.가변1 = ToIntOrDefault(arr[14], 1);
            s.가변2 = ToIntOrDefault(arr[15], 1);
            s.가변3 = ToIntOrDefault(arr[16], 1);
            s.가변4 = ToIntOrDefault(arr[17], 1);

            s.VarAdd = arr[18];
            s.위험보험료 = ToDoubleOrDefault(arr[19], 0);
            s.정기위험보험료 = ToDoubleOrDefault(arr[20], 0);
            s.S = ToDoubleOrDefault(arr[21], 0);
            s.Min_S = ToDoubleOrDefault(arr[22], 0);
            s.ErrorMessage = arr[23];

            return s;
        }

        private ChkExprs ToChkExprs(string[] arr)
        {
            ChkExprs chkExprs = new ChkExprs();

            chkExprs.회사 = arr[0];
            chkExprs.조건1 = CompileBool(arr[1]);
            chkExprs.조건2 = CompileBool(arr[2]);
            chkExprs.조건3 = CompileBool(arr[3]);
            chkExprs.조건4 = CompileBool(arr[4]);

            chkExprs.산출항목 = arr[5];
            chkExprs.산출수식 = CompileDouble(arr[6]);

            return chkExprs;
        }

    }
}
