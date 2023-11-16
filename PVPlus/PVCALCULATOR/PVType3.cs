using Flee.PublicTypes;
using PVPlus.RULES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVPlus.PVCALCULATOR
{
    public class PVType3 : PVCalculator
    {
        //월만기형 모성담보, 0시점 위험률 Rate[0]값을 순보험료로 사용
        public PVType3(LineInfo line) : base(line)
        {

        }

        public override double Get순보험료(int n, int m, int t, int freq)
        {
            return c.RateSegments_급부.First()[0];
        }

        public override double Get영업보험료(int n, int m, int t, int freq)
        {
            double 순보험료 = Get순보험료(n, m, t, freq);
            double 영업보험료 = 순보험료 / (1 - ex.Alpha_P - ex.Beta_P - ex.Gamma - ex.Ce);

            return 영업보험료;
        }

        public override double Get준비금(int n, int m, int t, int freq)
        {
            if (t == 0) return Get순보험료(n, m, t, freq);
            else return 0;
        }
    }

}
