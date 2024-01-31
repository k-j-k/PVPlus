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
        //납입면제특약
        public PVType3(LineInfo line) : base(line)
        {

        }

        public override double Get순보험료(int n, int m, int t, int freq)
        {
            double NP = 0;
            double payCnt = Get연납입횟수(freq);

            double NNx_월납입자 = GetNNx(c.Nx_납입자, c.Dx_납입자, 1, 0, n);
            double NNx_월유지자 = GetNNx(c.Nx_유지자, c.Dx_유지자, 1, 0, n);
            double NNx_납입자 = GetNNx(c.Nx_납입자, c.Dx_납입자, freq, 0, m);

            if (freq == 99)
            {
                NP = (c.Mx_급부[0] - c.Mx_급부[n]) / c.Dx_납입자[0];
            }
            else
            {
                NP = (NNx_월유지자 - NNx_월납입자) / NNx_납입자 * (12.0 / payCnt);
            }

            return NP;
        }

        public override double Get준비금(int n, int m, int t, int freq)
        {
            double 순보험료 = Get순보험료(n, m, t, freq);
            double payCnt = Get연납입횟수(freq);

            double NNx_납입자 = GetNNx(c.Nx_납입자, c.Dx_납입자, freq, t, m);
            double NNx_유지자 = GetNNx(c.Nx_유지자, c.Dx_유지자, freq, t, m);

            double 분자 = 0;
            double 분모 = 1.0;

            double 분자Out = NNx_유지자 - NNx_납입자;
            double 분자In = (m > 0 && t <= m) ? 순보험료 * NNx_납입자 : 0;

            if (freq == 99)
            {
                분자 = 분자Out;
                분모 = c.Dx_유지자[t];
            }
            else
            {
                분자 = payCnt * (분자Out - 분자In);
                분모 = c.Dx_유지자[t];
            }

            return 분자 / 분모;
        }
    }

}
