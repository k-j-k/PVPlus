using Flee.PublicTypes;
using PVPlus.RULES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVPlus.PVCALCULATOR
{
    public class PVType12 : PVCalculator
    {
        public PVType12(LineInfo line) : base(line)
        {

        }

        public override double Get영업보험료(int n, int m, int t, int freq)
        {
            double 분자 = 0;
            double 분모 = 1.0;

            double payCnt = Get연납입횟수(freq);
            double APV = Get연금현가(c.Nx_납입자, c.Dx_납입자, freq, 0, m);
            double NPSTD = Get기준연납순보험료(n, m, t, 12);
            double NP = Get순보험료(n, m, t, freq);
            double BetaPPerGP = GetBeta보험료(n, m, t, freq);

            if (freq == 99)
            {
                분자 = NP;
                분모 = 1 - ex.Alpha_P - ex.Alpha2_P - ex.Beta_P - ex.Gamma - ex.Ce;
            }
            else
            {
                분자 = NP + (ex.Alpha_S + NPSTD * ex.Alpha_Statutory) / (payCnt * APV) + (ex.Beta_S / payCnt);
                분모 = (1.0 - ex.Alpha_P / APV - ex.Alpha2_P - ex.Beta_P - ex.Gamma - ex.Ce - payCnt * BetaPPerGP);
            }

            return 분자 / 분모;
        }

        public override double Get준비금(int n, int m, int t, int freq)
        {
            double GP = Get영업보험료(n, m, t, freq);
            double BetaNP = GetBeta순보험료(n, m, t, freq);
            double payCnt = Get연납입횟수(freq);
            double NNx_납입자 = GetNNx(c.Nx_납입자, c.Dx_납입자, freq, t, m);
            double NNx_납후유지자 = GetNNx(c.Nx_유지자, c.Dx_유지자, 12, Math.Max(m, t), n);

            double 분자 = 0;
            double 분모 = 1.0;

            double 분자Out = (c.Mx_급부[t] - c.Mx_급부[n]) + GP * ex.Betaprime_P * NNx_납후유지자;
            double 분자In = (m > 0 && t <= m) ? BetaNP * payCnt * NNx_납입자 : 0;

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

        public override double GetBeta순보험료(int n, int m, int t, int freq)
        {
            return Get순보험료(n, m, t, freq) + Get영업보험료(n, m, t, freq) * GetBeta보험료(n, m, t, freq);
        }

        public double GetBeta보험료(int n, int m, int t, int freq)
        {
            //납방별 영업보험료 1원당 적용되는 납후유지비에 의한 순보험료 증가액
            double betaPrime = ex.Betaprime_P;
            double payCnt = Get연납입횟수(freq);

            double 분자 = GetNNx(c.Nx_유지자, c.Dx_유지자, 12, m, n);
            double 분모 = payCnt * GetNNx(c.Nx_납입자, c.Dx_납입자, freq, 0, m);

            return betaPrime * 분자 / 분모;
        }

    }
}
