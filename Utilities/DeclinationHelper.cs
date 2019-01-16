using System;
namespace Scarlet.Utilities
{
    public static class DeclinationHelper
    {
        private static double[,] c = new double[13, 13]; // Gauss wmm_coefficients of geomagnetic model
        private static double[,] cd = new double[13, 13]; // Gauss wmm_coefficients of secular geomagnetic model
        private static double[,] tc = new double[13, 13]; // time adjusted wmm_coefficients
        private static double[,] dp = new double[13, 13]; // theta derivative of p(n,m) (unnormalized)
        private static double[] snorm = new double[169]; // Schmidt normalization factors
        private static double[] sp = new double[13]; // sine of spherical coordinates
        private static double[] cp = new double[13];
        private static double[] fn = new double[13];
        private static double[] fm = new double[13];
        private static double[] pp = new double[13];
        private static double[,] k = new double[13, 13];
        private static double[] wmm_coefficients = new double[]
        {
            1, 0, -29438.2, 0.0, 7.0, 0.0, 1, 1, -1493.5,
                4796.3, 9.0, -30.2, 2, 0, -2444.5, 0.0, -11.0, 0.0, 2, 1, 3014.7, -2842.4,
                -6.2, -29.6, 2, 2, 1679.0, -638.8, 0.3, -17.3, 3, 0, 1351.8, 0.0, 2.4, 0.0,
                3, 1, -2351.6, -113.7, -5.7, 6.5, 3, 2, 1223.6, 246.5, 2.0, -0.8, 3, 3, 582.3,
                -537.4, -11.0, -2.0, 4, 0, 907.5, 0.0, -0.8, 0.0, 4, 1, 814.8, 283.3, -0.9,
                -0.4, 4, 2, 117.8, -188.6, -6.5, 5.8, 4, 3, -335.6, 180.7, 5.2, 3.8, 4, 4,
                69.7, -330.0, -4.0, -3.5, 5, 0, -232.9, 0.0, -0.3, 0.0, 5, 1, 360.1, 46.9,
                0.6, 0.2, 5, 2, 191.7, 196.5, -0.8, 2.3, 5, 3, -141.3, -119.9, 0.1, -0.0, 5,
                4, -157.2, 16.0, 1.2, 3.3, 5, 5, 7.7, 100.6, 1.4, -0.6, 6, 0, 69.4, 0.0, -0.8,
                0.0, 6, 1, 67.7, -20.1, -0.5, 0.3, 6, 2, 72.3, 32.8, -0.1, -1.5, 6, 3, -129.1,
                59.1, 1.6, -1.2, 6, 4, -28.4, -67.1, -1.6, 0.4, 6, 5, 13.6, 8.1, 0.0, 0.2, 6,
                6, -70.3, 61.9, 1.2, 1.3, 7, 0, 81.7, 0.0, -0.3, 0.0, 7, 1, -75.9, -54.3, -0.2,
                0.6, 7, 2, -7.1, -19.5, -0.3, 0.5, 7, 3, 52.2, 6.0, 0.9, -0.8, 7, 4, 15.0,
                24.5, 0.1, -0.2, 7, 5, 9.1, 3.5, -0.6, -1.1, 7, 6, -3.0, -27.7, -0.9, 0.1,
                7, 7, 5.9, -2.9, 0.7, 0.2, 8, 0, 24.2, 0.0, -0.1, 0.0, 8, 1, 8.9, 10.1, 0.2,
                -0.4, 8, 2, -16.9, -18.3, -0.2, 0.6, 8, 3, -3.1, 13.3, 0.5, -0.1, 8, 4, -20.7,
                -14.5, -0.1, 0.6, 8, 5, 13.3, 16.2, 0.4, -0.2, 8, 6, 11.6, 6.0, 0.4, -0.5, 8,
                7, -16.3, -9.2, -0.1, 0.5, 8, 8, -2.1, 2.4, 0.4, 0.1, 9, 0, 5.5, 0.0, -0.1, 0.0,
                9, 1, 8.8, -21.8, -0.1, -0.3, 9, 2, 3.0, 10.7, -0.0, 0.1, 9, 3, -3.2, 11.8, 0.4,
                -0.4, 9, 4, 0.6, -6.8, -0.4, 0.3, 9, 5, -13.2, -6.9, 0.0, 0.1, 9, 6, -0.1, 7.9,
                0.3, -0.0, 9, 7, 8.7, 1.0, 0.0, -0.1, 9, 8, -9.1, -3.9, -0.0, 0.5, 9, 9, -10.4,
                8.5, -0.3, 0.2, 10, 0, -2.0, 0.0, 0.0, 0.0, 10, 1, -6.1, 3.3, -0.0, 0.0, 10, 2,
                0.2, -0.4, -0.1, 0.1, 10, 3, 0.6, 4.6, 0.2, -0.2, 10, 4, -0.5, 4.4, -0.1, 0.1,
                10, 5, 1.8, -7.9, -0.2, -0.1, 10, 6, -0.7, -0.6, -0.0, 0.1, 10, 7, 2.2, -4.2,
                -0.1, -0.0, 10, 8, 2.4, -2.9, -0.2, -0.1, 10, 9, -1.8, -1.1, -0.1, 0.2, 10, 10,
                -3.6, -8.8, -0.0, -0.0, 11, 0, 3.0, 0.0, -0.0, 0.0, 11, 1, -1.4, -0.0, 0.0, 0.0,
                11, 2, -2.3, 2.1, -0.0, 0.1, 11, 3, 2.1, -0.6, 0.0, 0.0, 11, 4, -0.8, -1.1, -0.0,
                0.1, 11, 5, 0.6, 0.7, -0.1, -0.0, 11, 6, -0.7, -0.2, 0.0, -0.0, 11, 7, 0.1, -2.1,
                -0.0, 0.1, 11, 8, 1.7, -1.5, -0.0, -0.0, 11, 9, -0.2, -2.6, -0.1, -0.1, 11, 10,
                0.4, -2.0, -0.0, -0.0, 11, 11, 3.5, -2.3, -0.1, -0.1, 12, 0, -2.0, 0.0, 0.0,
                0.0, 12, 1, -0.1, -1.0, 0.0, -0.0, 12, 2, 0.5, 0.3, -0.0, 0.0, 12, 3, 1.2, 1.8,
                0.0, -0.1, 12, 4, -0.9, -2.2, -0.1, 0.1, 12, 5, 0.9, 0.3, -0.0, -0.0, 12, 6, 0.1,
                0.7, 0.0, 0.0, 12, 7, 0.6, -0.1, -0.0, -0.0, 12, 8, -0.4, 0.3, 0.0, 0.0, 12, 9,
                -0.5, 0.2, -0.0, 0.0, 12, 10, 0.2, -0.9, -0.0, -0.0, 12, 11, -0.9, -0.2, -0.0,
                0.0, 12, 12, -0.0, 0.8, -0.1, -0.1
        };

        private static int maxOrd = 12;
        private static double a = 6378.137;
        private static double b = 6356.7523142;

        private static double re = 6371.2;
        private static double a2 = a * a;
        private static double b2 = b * b;
        private static double c2 = a2 - b2;
        private static double a4 = a2 * a2;
        private static double b4 = b2 * b2;
        private static double c4 = a4 - b4;

        static DeclinationHelper()
        {
            // initialize constants
            sp[0] = 0.0;
            cp[0] = snorm[0] = pp[0] = 1.0;
            dp[0, 0] = 0.0;
            //Read WMM COF coefficients
            for (int i = 3; i < wmm_coefficients.Length; i += 6)
            {
                int n = (int)(wmm_coefficients[i]);
                int m = (int)(wmm_coefficients[i + 1]);
                double gnm = wmm_coefficients[i + 2];
                double hnm = wmm_coefficients[i + 3];
                double dgnm = wmm_coefficients[i + 4];
                double dhnm = wmm_coefficients[i + 5];
                if (m <= n)
                {
                    c[m, n] = gnm;
                    cd[m, n] = dgnm;
                    if (m != 0)
                    {
                        c[n, m - 1] = hnm;
                        cd[n, m - 1] = dhnm;
                    }
                }
            }
            // Convert Schmidt normalized Gauss coefficients to unnormalized
            snorm[0] = 1.0;
            for (int n = 1; n <= maxOrd; n++)
            {

                snorm[n] = snorm[n - 1] * (2 * n - 1) / n;
                int j = 2;

                for (int m = 0, D1 = 1, D2 = (n - m + D1) / D1; D2 > 0; D2--, m += D1)
                {
                    k[m, n] = (double)(((n - 1) * (n - 1)) - (m * m)) / (double)((2 * n - 1) * (2 * n - 3));
                    if (m > 0)
                    {
                        double flnmj = ((n - m + 1) * j) / (double)(n + m);
                        snorm[n + m * 13] = snorm[n + (m - 1) * 13] * Math.Sqrt(flnmj);
                        j = 1;
                        c[n, m - 1] = snorm[n + m * 13] * c[n, m - 1];
                        cd[n, m - 1] = snorm[n + m * 13] * cd[n, m - 1];
                    }
                    c[m, n] = snorm[n + m * 13] * c[m, n];
                    cd[m, n] = snorm[n + m * 13] * cd[m, n];
                }   //for(m...)

                fn[n] = (n + 1);
                fm[n] = n;

            }   //for(n...)

            k[1, 1] = 0.0;

            double otime; double oalt; double olat; double olon;
            otime = oalt = olat = olon = -1000.0;
        }
        public static double calcGeoMag(double lat, double lon, double year = 2019, double altitude = 0)
        {
            double glat = lat;
            double glon = lon;
            double alt = altitude;
            double time = year;

            double dt = time - 2015;
            double pi = Math.PI;
            double dtr = pi / 180.0;
            double rlon = glon * dtr;
            double rlat = glat * dtr;
            double srlon = Math.Sin(rlon);
            double srlat = Math.Sin(rlat);
            double crlon = Math.Cos(rlon);
            double crlat = Math.Cos(rlat);
            double srlat2 = srlat * srlat;
            double crlat2 = crlat * crlat;



            double st = 0;
            double ct = 0;
            double ca = 0;
            double sa = 0;
            double r = 0;
            sp[1] = srlon;
            cp[1] = crlon;

            if (Math.Abs(alt - -1000) <= 1e-6 || Math.Abs(glat - -1000) <= 1e-6)
            {
                double q = Math.Sqrt(a2 - c2 * srlat2);
                double q1 = alt * q;
                double q2 = ((q1 + a2) / (q1 + b2)) * ((q1 + a2) / (q1 + b2));
                ct = srlat / Math.Sqrt(q2 * crlat2 + srlat2);
                st = Math.Sqrt(1.0 - ct * ct);
                double r2 = ((alt * alt) + 2.0 * q1 + (a4 - c4 * srlat2) / (q * q));
                r = Math.Sqrt(r2);
                double d = Math.Sqrt(a2 * crlat2 + b2 * srlat2);
                ca = (alt + d) / r;
                sa = c2 * crlat * srlat / (r * d);
            }
            if (Math.Abs(glon - -1000) <= 1e-6)
            {
                for (int n = 2; n <= 12; n++)
                {
                    sp[n] = sp[1] * cp[n - 1] + cp[1] * sp[n - 1];
                    cp[n] = cp[1] * cp[n - 1] - sp[1] * sp[n - 1];
                }
            }

            double aor = re / r;
            double ar = aor * aor;
            double br = 0; double bt = 0; double bp = 0; double bpp = 0;

            for (int n = 1; n <= 12; n++)
            {
                ar = ar * aor;
                for (int m = 0, D3 = 1, D4 = (n + m + D3) / D3; D4 > 0; D4--, m += D3)
                {
                    if (Math.Abs(alt - -1000) <= 1e-6 || Math.Abs(glat - -1000) <= 1e-6)
                    {
                        if (n == m)
                        {
                            snorm[n + m * 13] = st * snorm[n - 1 + (m - 1) * 13];
                            dp[m, n] = st * dp[m - 1, n - 1] + ct * snorm[n - 1 + (m - 1) * 13];
                        }
                        if (n == 1 && m == 0)
                        {
                            snorm[n + m * 13] = ct * snorm[n - 1 + m * 13];
                            dp[m, n] = ct * dp[m, n - 1] - st * snorm[n - 1 + m * 13];
                        }
                        if (n > 1 && n != m)
                        {
                            if (m > n - 2)
                            {
                                snorm[n - 2 + m * 13] = 0.0;
                            }
                            if (m > n - 2)
                            {
                                dp[m, n - 2] = 0.0;
                            }
                            snorm[n + m * 13] = ct * snorm[n - 1 + m * 13] - k[m, n] * snorm[n - 2 + m * 13];
                            dp[m, n] = ct * dp[m, n - 1] - st * snorm[n - 1 + m * 13] - k[m, n] * dp[m, n - 2];
                        }
                    }

                    //TIME ADJUST THE GAUSS COEFFICIENTS
                    tc[m, n] = c[m, n] + dt * cd[m, n];
                    if (m != 0)
                    {
                        tc[n, m - 1] = c[n, m - 1] + dt * cd[n, m - 1];
                    }

                    //ACCUMULATE TERMS OF THE SPHERICAL HARMONIC EXPANSIONS

                    double temp1, temp2;
                    double par = ar * snorm[n + m * 13];
                    if (m == 0)
                    {
                        temp1 = tc[m, n] * cp[m];
                        temp2 = tc[m, n] * sp[m];
                    }
                    else
                    {
                        temp1 = tc[m, n] * cp[m] + tc[n, m - 1] * sp[m];
                        temp2 = tc[m, n] * sp[m] - tc[n, m - 1] * cp[m];
                    }

                    bt = bt - ar * temp1 * dp[m, n];
                    bp += (fm[m] * temp2 * par);
                    br += (fn[n] * temp1 * par);

                }
            }

            if (st == 0.0)
            {
                bp = bpp;
            }
            else
            {
                bp /= st;
            }

            double bx = -bt * ca - br * sa;
            double by = bp;
            double bz = bt * sa - br * ca;

            return (Math.Atan2(by, bx) / dtr);
        }
    }
}
