using Hangfire;

namespace Services
{
    public class CurrencyConverter
    {
        private static List<CurrencyExchangeRate>? currencyExchangeRates;

        public CurrencyConverter()
        {
            if (currencyExchangeRates is null)
            {
                UpdateRates();
                RecurringJob.AddOrUpdate("UpdateCurrencyRates", () => UpdateRates(), Cron.Daily);
            }
        }

        public static void UpdateRates()
        {
            using HttpClient HttpClient = new();

            var response = HttpClient.GetAsync("https://api.monobank.ua/bank/currency").Result;
            if (response.IsSuccessStatusCode)
            {
                currencyExchangeRates = response.Content.ReadFromJsonAsync<List<CurrencyExchangeRate>>().Result;
            }
        }

        public double Convert(double value, string from, string to)
        {
            try
            {
                CurrencyCode codeA = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), from.ToUpper());
                CurrencyCode codeB = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), to.ToUpper());

                var rate = currencyExchangeRates?.Find(r => (r.CurrencyCodeA == (int)codeA && r.CurrencyCodeB == (int)codeB)
                            || (r.CurrencyCodeB == (int)codeA && r.CurrencyCodeA == (int)codeB));
                if (rate != null)
                {
                    double? result;

                    if (rate.CurrencyCodeA == (int)codeA)
                    {
                        result = value * rate.RateSell ?? value * rate.RateCross;
                    }
                    else
                    {
                        result = value / rate.RateSell ?? value / rate.RateCross;
                    }

                    return result ??= -1;
                }
                else
                {
                    rate = currencyExchangeRates?.Find(r => r.CurrencyCodeA == (int)codeA && r.CurrencyCodeB == (int)CurrencyCode.UAH);
                    double? result;

                    if (rate != null)
                    {
                        var inUAH = value * rate.RateSell ?? value * rate.RateCross;
                        result = inUAH;

                        rate = currencyExchangeRates?.Find(r => r.CurrencyCodeA == (int)codeB && r.CurrencyCodeB == (int)CurrencyCode.UAH);

                        if (rate != null)
                        {
                            result = result / rate.RateSell ?? result / rate.RateCross;
                            return result ??= -1;
                        }
                    }
                }
                return -1;
            }
            catch
            {
                return value;
            }
        }

        private class CurrencyExchangeRate
        {
            public int CurrencyCodeA { get; set; }
            public int CurrencyCodeB { get; set; }
            public long Date { get; set; }
            public double? RateBuy { get; set; }
            public double? RateSell { get; set; }
            public double? RateCross { get; set; }
        }

        public enum CurrencyCode
        {
            AED = 784, // United Arab Emirates Dirham
            AFN = 971, // Afghan Afghani
            ALL = 8,   // Albanian Lek
            AMD = 51,  // Armenian Dram
            ANG = 532, // Netherlands Antillean Guilder
            AOA = 973, // Angolan Kwanza
            ARS = 32,  // Argentine Peso
            AUD = 36,  // Australian Dollar
            AWG = 533, // Aruban Florin
            AZN = 944, // Azerbaijani Manat
            BAM = 977, // Bosnia-Herzegovina Convertible Mark
            BBD = 52,  // Barbadian Dollar
            BDT = 50,  // Bangladeshi Taka
            BGN = 975, // Bulgarian Lev
            BHD = 48,  // Bahraini Dinar
            BIF = 108, // Burundian Franc
            BMD = 60,  // Bermudian Dollar
            BND = 96,  // Brunei Dollar
            BOB = 68,  // Bolivian Boliviano
            BOV = 984, // Bolivian Mvdol (funds code)
            BRL = 986, // Brazilian Real
            BSD = 44,  // Bahamian Dollar
            BTN = 64,  // Bhutanese Ngultrum
            BWP = 72,  // Botswana Pula
            BYN = 933, // Belarusian Ruble
            BZD = 84,  // Belize Dollar
            CAD = 124, // Canadian Dollar
            CDF = 976, // Congolese Franc
            CHE = 947, // WIR Euro (complementary currency)
            CHF = 756, // Swiss Franc
            CHW = 948, // WIR Franc (complementary currency)
            CLF = 990, // Unidad de Fomento (funds code)
            CLP = 152, // Chilean Peso
            CNY = 156, // Chinese Yuan
            COP = 170, // Colombian Peso
            COU = 970, // Colombian Real Value Unit (funds code)
            CRC = 188, // Costa Rican Colón
            CUC = 931, // Cuban Convertible Peso
            CUP = 192, // Cuban Peso
            CVE = 132, // Cape Verdean Escudo
            CZK = 203, // Czech Republic Koruna
            DJF = 262, // Djiboutian Franc
            DKK = 208, // Danish Krone
            DOP = 214, // Dominican Peso
            DZD = 12,  // Algerian Dinar
            EGP = 818, // Egyptian Pound
            ERN = 232, // Eritrean Nakfa
            ETB = 230, // Ethiopian Birr
            EUR = 978, // Euro
            FJD = 242, // Fijian Dollar
            FKP = 238, // Falkland Islands Pound
            FOK = 978, // Faroese Króna (предложено)
            GBP = 826, // British Pound Sterling
            GEL = 981, // Georgian Lari
            GGP = 826, // Guernsey Pound (предложено)
            GHS = 936, // Ghanaian Cedi
            GIP = 292, // Gibraltar Pound
            GMD = 270, // Gambian Dalasi
            GNF = 324, // Guinean Franc
            GTQ = 320, // Guatemalan Quetzal
            GYD = 328, // Guyanaese Dollar
            HKD = 344, // Hong Kong Dollar
            HNL = 340, // Honduran Lempira
            HRK = 191, // Croatian Kuna
            HTG = 332, // Haitian Gourde
            HUF = 348, // Hungarian Forint
            IDR = 360, // Indonesian Rupiah
            ILS = 376, // Israeli New Shekel
            IMP = 826, // Isle of Man Pound (предложено)
            INR = 356, // Indian Rupee
            IQD = 368, // Iraqi Dinar
            IRR = 364, // Iranian Rial
            ISK = 352, // Icelandic Króna
            JEP = 826, // Jersey Pound (предложено)
            JMD = 388, // Jamaican Dollar
            JOD = 400, // Jordanian Dinar
            JPY = 392, // Japanese Yen
            KES = 404, // Kenyan Shilling
            KGS = 417, // Kyrgyzstani Som
            KHR = 116, // Cambodian Riel
            KID = 981, // Kiribati Dollar (предложено)
            KMF = 174, // Comoro Franc
            KRW = 410, // South Korean Won
            KWD = 414, // Kuwaiti Dinar
            KYD = 136, // Cayman Islands Dollar
            KZT = 398, // Kazakhstani Tenge
            LAK = 418, // Lao Kip
            LBP = 422, // Lebanese Pound
            LKR = 144, // Sri Lankan Rupee
            LRD = 430, // Liberian Dollar
            LSL = 426, // Lesotho Loti
            LYD = 434, // Libyan Dinar
            MAD = 504, // Moroccan Dirham
            MDL = 498, // Moldovan Leu
            MGA = 969, // Malagasy Ariary
            MKD = 807, // Macedonian Denar
            MMK = 104, // Burmese Kyat
            MNT = 496, // Mongolian Tögrög
            MOP = 446, // Macanese Pataca
            MRU = 929, // Mauritanian Ouguiya
            MUR = 480, // Mauritian Rupee
            MVR = 462, // Maldivian Rufiyaa
            MWK = 454, // Malawian Kwacha
            MXN = 484, // Mexican Peso
            MYR = 458, // Malaysian Ringgit
            MZN = 943, // Mozambican Metical
            NAD = 516, // Namibian Dollar
            NGN = 566, // Nigerian Naira
            NIO = 558, // Nicaraguan Córdoba
            NOK = 578, // Norwegian Krone
            NPR = 524, // Nepalese Rupee
            NZD = 554, // New Zealand Dollar
            OMR = 512, // Omani Rial
            PAB = 590, // Panamanian Balboa
            PEN = 604, // Peruvian Nuevo Sol
            PGK = 598, // Papua New Guinean Kina
            PHP = 608, // Philippine Peso
            PKR = 586, // Pakistani Rupee
            PLN = 985, // Polish Złoty
            PYG = 600, // Paraguayan Guaraní
            QAR = 634, // Qatari Riyal
            RON = 946, // Romanian Leu
            RSD = 941, // Serbian Dinar
            RUB = 643, // Russian Ruble
            RWF = 646, // Rwandan Franc
            SAR = 682, // Saudi Riyal
            SBD = 90,  // Solomon Islands Dollar
            SCR = 690, // Seychellois Rupee
            SDG = 938, // Sudanese Pound
            SEK = 752, // Swedish Krona
            SGD = 702, // Singapore Dollar
            SHP = 654, // Saint Helena Pound
            SLL = 694, // Sierra Leonean Leone
            SOS = 706, // Somali Shilling
            SRD = 968, // Surinamese Dollar
            SSP = 728, // South Sudanese Pound
            STN = 930, // São Tomé and Príncipe Dobra
            SVC = 222, // Salvadoran Colón
            SYP = 760, // Syrian Pound
            SZL = 748, // Swazi Lilangeni
            THB = 764, // Thai Baht
            TJS = 972, // Tajikistani Somoni
            TMT = 934, // Turkmenistani Manat
            TND = 788, // Tunisian Dinar
            TOP = 776, // Tongan Paʻanga
            TRY = 949, // Turkish Lira
            TTD = 780, // Trinidad and Tobago Dollar
            TWD = 901, // New Taiwan Dollar
            TZS = 834, // Tanzanian Shilling
            UAH = 980, // Ukrainian Hryvnia
            UGX = 800, // Ugandan Shilling
            USD = 840, // United States Dollar
            USN = 997, // United States Dollar (Next day) (funds code)
            UYI = 940, // Uruguay Peso en Unidades Indexadas (funds code)
            UYU = 858, // Uruguayan Peso
            UYW = 927, // Unidad previsional
            UZS = 860, // Uzbekistan Som
            VES = 928, // Venezuelan Bolívar Soberano
            VND = 704, // Vietnamese Đồng
            VUV = 548, // Vanuatu Vatu
            WST = 882, // Samoan Tala
            XAF = 950, // Central African CFA Franc
            XAG = 961, // Silver (one troy ounce)
            XAU = 959, // Gold (one troy ounce)
            XBA = 955, // European Composite Unit (EURCO) (bond market unit)
            XBB = 956, // European Monetary Unit (E.M.U.-6) (bond market unit)
            XBC = 957, // European Unit of Account 9 (E.U.A.-9) (bond market unit)
            XBD = 958, // European Unit of Account 17 (E.U.A.-17) (bond market unit)
            XCD = 951, // East Caribbean Dollar
            XDR = 960, // Special Drawing Rights
            XOF = 952, // West African CFA franc
            XPD = 964, // Palladium (one troy ounce)
            XPF = 953, // CFP Franc
            XPT = 962, // Platinum (one troy ounce)
            XSU = 994, // SUCRE
            XTS = 963, // Code reserved for testing purposes
            XUA = 965, // ADB Unit of Account
            XXX = 999, // No currency
            YER = 886, // Yemeni Rial
            ZAR = 710, // South African Rand
            ZMW = 967, // Zambian Kwacha
            ZWL = 932  // Zimbabwean Dollar
        }
    }
}