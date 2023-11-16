using Flee.PublicTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVPlus.RULES
{
    public class SInfo
    {
        public string SKey { get; set; }
        public string 상품코드 { get; set; }
        public string 담보코드 { get; set; }
        public int GroupKey1 { get; set; }
        public int GroupKey2 { get; set; }
        public int GroupKey3 { get; set; }

        public int x { get; set; }
        public int n { get; set; }
        public int m { get; set; }
        public int 성별 { get; set; }
        public int 급수 { get; set; }
        public int 운전 { get; set; }
        public int 금액 { get; set; }
        public int 사고연령 { get; set; }
        public int 가변1 { get; set; }
        public int 가변2 { get; set; }
        public int 가변3 { get; set; }
        public int 가변4 { get; set; }

        public IGenericExpression<double> 위험보험료Expr { get; set; }
        public IGenericExpression<double> 정기위험보험료Expr { get; set; }
        public IGenericExpression<double> SExpr { get; set; }

        public string VarAdd { get; set; }
        public double 위험보험료 { get; set; }
        public double 정기위험보험료 { get; set; }
        public double S { get; set; }
        public double Min_S { get; set; }

        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            List<string> outItemList = new List<string>();

            foreach (var s in GetType().GetProperties())
            {
                if (s.PropertyType == typeof(string) || s.PropertyType == typeof(int))
                {
                    outItemList.Add(s.GetValue(this).ToString());
                }
                else if (s.PropertyType == typeof(double))
                {
                    outItemList.Add(Convert.ToDouble(s.GetValue(this)).ToString("F15"));
                }
            }

            return string.Join("\t", outItemList);
        }
    }

}
