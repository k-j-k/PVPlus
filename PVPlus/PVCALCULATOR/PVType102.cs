using PVPlus.RULES;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVPlus.PVCALCULATOR
{
    public class PVType102 : PVType11
    {
        // 우체국 든든건강종신
        public PVType102(LineInfo line) : base(line)
        {
            for(int i = 0; i < CommutationTable.MAXSIZE; i++)
            {
                c.MxSegments_급부합계[i] =
                    c.MxSegments_급부[0][i] * (c.Rate_r3[i] == 0 ? 1 : c.Rate_r2[i] / c.Rate_r3[i])
                    + c.MxSegments_급부[1][i]
                    + c.MxSegments_급부[2][i]
                    + c.MxSegments_급부[3][i]
                    + c.MxSegments_급부[4][i]
                    + c.MxSegments_급부[5][i]
                    + c.MxSegments_급부[6][i] * (c.Rate_r3[i] == 0 ? 1 : c.Rate_r2[i] / c.Rate_r3[i]);

                c.Mx_급부[i] = c.MxSegments_급부합계[i] + c.Mx_납입자급부[i] + c.Mx_납입면제자급부[i];
            }
        }

        public override double Get순보험료(int n, int m, int t, int freq)
        {
            return base.Get순보험료(n, m, t, freq);
        }

        public override double GetBeta순보험료(int n, int m, int t, int freq)
        {
            return base.GetBeta순보험료(n, m, t, freq);
        }

        public override double Get준비금(int n, int m, int t, int freq)
        {
            if (t == n) return 1;

            double BetaNP = GetBeta순보험료(n, m, t, freq);
            double payCnt = Get연납입횟수(freq);
            double NNx_납입자 = GetNNx(c.Nx_납입자, c.Dx_납입자, freq, t, m);
            double NNx_납후유지자 = GetNNx(c.Nx_유지자, c.Dx_유지자, 12, Math.Max(m, t), n);

            bool Is발생자 = (int)variables["S6"] > 0 ? true : false;
            double[] Dstar = c.GetDx(c.Rate_r2);
            double VE = 0;

            if (Is발생자)
            {
                VE = (c.MxSegments_급부[0][t] - c.MxSegments_급부[0][n] + c.MxSegments_급부[6][t] - c.MxSegments_급부[6][n] + ex.Betaprime_S * NNx_납후유지자) / c.Dx_유지자[t];
            }
            else
            {
                VE = (c.Mx_급부[t] - c.Mx_급부[n]) / Dstar[t] + ex.Betaprime_S * NNx_납후유지자 / c.Dx_유지자[t] - BetaNP * payCnt * NNx_납입자 / Dstar[t];
            }


            return VE;

        }
    }
}
