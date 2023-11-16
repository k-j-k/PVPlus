using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PVPlus.RULES;

namespace PVPlus.PVCALCULATOR
{
    //Special PVTypes
    public class PVSubstandard : PVCalculator
    {
        public PVCalculator sub;
        public PVCalculator norm;

        public PVSubstandard(PVCalculator sub, PVCalculator norm) : base()
        {

            this.sub = sub;
            this.norm = norm;

            line = norm.line;
            variables = norm.variables;
            c = norm.c;
            ex = norm.ex;

            productRule = norm.productRule;
            riderRule = norm.riderRule;
            companyRule = Configure.CompanyRule;

            가입금액 = (double)variables["Amount"];
            Min_s = 0;

            helper.cal = this;
        }

        public override double Get순보험료(int n, int m, int t, int freq)
        {
            return sub.Get순보험료(n, m, t, freq) - norm.Get순보험료(n, m, t, freq);
        }
        public override double GetBeta순보험료(int n, int m, int t, int freq)
        {
            return sub.Get순보험료(n, m, t, freq) - norm.Get순보험료(n, m, t, freq);
        }
        public override double Get위험보험료(int n, int m, int t, int freq)
        {
            return sub.Get위험보험료(n, m, t, freq) - norm.Get위험보험료(n, m, t, freq);
        }
        public override double Get영업보험료(int n, int m, int t, int freq)
        {
            return sub.Get순보험료(n, m, t, freq) - norm.Get순보험료(n, m, t, freq);
        }
        public override double Get준비금(int n, int m, int t, int freq)
        {
            return sub.Get준비금(n, m, t, freq) - norm.Get준비금(n, m, t, freq);
        }
    }

    public class PVSubStandardRound : PVSubstandard
    {
        //표준하체 Round(S배위험률 요율값) - Round(1배위험률 요율값) 
        public PVSubStandardRound(PVCalculator sub, PVCalculator norm) : base(sub, norm)
        {
        }

        public override double Get순보험료(int n, int m, int t, int freq)
        {
            return helper.Round2(sub.Get순보험료(n, m, t, freq) * 가입금액) / 가입금액 - helper.Round2(norm.Get순보험료(n, m, t, freq) * 가입금액) / 가입금액;
        }
        public override double GetBeta순보험료(int n, int m, int t, int freq)
        {
            return helper.Round2(sub.Get순보험료(n, m, t, freq) * 가입금액) / 가입금액 - helper.Round2(norm.Get순보험료(n, m, t, freq) * 가입금액) / 가입금액;
        }
        public override double Get위험보험료(int n, int m, int t, int freq)
        {
            return helper.Round2(sub.Get위험보험료(n, m, t, freq) * 가입금액) / 가입금액 - helper.Round2(norm.Get위험보험료(n, m, t, freq) * 가입금액) / 가입금액;
        }
        public override double Get영업보험료(int n, int m, int t, int freq)
        {
            return helper.Round2(sub.Get순보험료(n, m, t, freq) * 가입금액) / 가입금액 - helper.Round2(norm.Get순보험료(n, m, t, freq) * 가입금액) / 가입금액;
        }
        public override double Get준비금(int n, int m, int t, int freq)
        {
            return helper.Round2(sub.Get준비금(n, m, t, freq) * 가입금액) / 가입금액 - helper.Round2(norm.Get준비금(n, m, t, freq) * 가입금액) / 가입금액;
        }
    }

    public class PVSubstandardSimple : PVSubstandard
    {
        //표준하체 Round(S배위험률 요율값)
        public PVSubstandardSimple(PVCalculator sub, PVCalculator norm) : base(sub, norm)
        {

        }

        public override double Get순보험료(int n, int m, int t, int freq)
        {
            return sub.Get순보험료(n, m, t, freq);
        }
        public override double GetBeta순보험료(int n, int m, int t, int freq)
        {
            return sub.Get순보험료(n, m, t, freq);
        }
        public override double Get위험보험료(int n, int m, int t, int freq)
        {
            return sub.Get위험보험료(n, m, t, freq);
        }
        public override double Get저축보험료(int n, int m, int t, int freq)
        {
            return sub.Get저축보험료(n, m, t, freq);
        }
        public override double Get영업보험료(int n, int m, int t, int freq)
        {
            return sub.Get순보험료(n, m, t, freq);
        }
        public override double Get준비금(int n, int m, int t, int freq)
        {
            return sub.Get준비금(n, m, t, freq);
        }
    }
}
