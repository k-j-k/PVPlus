using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVPlus.RULES
{
    public class RateRule
    {
        public string 위험률Key { get; set; }

        public string 위험률형태 { get; set; }
        public string 위험률명 { get; set; }
        public string 적용년월 { get; set; }
        public int 기간 { get; set; }

        //RateFactors
        public int 성별 { get; set; }
        public int 급수 { get; set; }
        public int 운전 { get; set; }
        public int 금액 { get; set; }
        public int 사고연령 { get; set; }
        public int 가변1 { get; set; }
        public int 가변2 { get; set; }
        public int 가변3 { get; set; }
        public int 가변4 { get; set; }
        //

        public double[] RateArr { get; set; }
    }
}
