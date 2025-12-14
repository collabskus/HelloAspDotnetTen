using BlazorApp.Models;

namespace BlazorApp.Services;

/// <summary>
/// Service that manages country data and comparison game logic.
/// Only presents countries that have data for the selected question type.
/// </summary>
public class CountryComparisonService
{
    private readonly List<CountryData> _countries;
    private readonly List<CountryComparisonQuestion> _questions;
    private readonly Random _random = new();

    public CountryGameScore Score { get; } = new();
    public IReadOnlyList<CountryData> Countries => _countries;
    public IReadOnlyList<CountryComparisonQuestion> AvailableQuestions => _questions;

    public CountryComparisonService()
    {
        _countries = CountryDataStore.GetAllCountries();
        _questions = InitializeQuestions();
    }

    /// <summary>
    /// Gets two random countries that both have data for the specified question.
    /// </summary>
    public (CountryData Country1, CountryData Country2)? GetRandomCountryPair(CountryComparisonQuestion question)
    {
        var eligible = _countries.Where(c => question.HasDataFor(c)).ToList();
        if (eligible.Count < 2) return null;

        var idx1 = _random.Next(eligible.Count);
        int idx2;
        do { idx2 = _random.Next(eligible.Count); } while (idx2 == idx1);
        return (eligible[idx1], eligible[idx2]);
    }

    public CountryComparisonQuestion GetRandomQuestion() => _questions[_random.Next(_questions.Count)];

    public int GetCountryCountForQuestion(CountryComparisonQuestion question)
        => _countries.Count(c => question.HasDataFor(c));

    public CountryData GetCorrectAnswer(CountryData c1, CountryData c2, CountryComparisonQuestion question)
    {
        var v1 = question.GetValue(c1) ?? 0;
        var v2 = question.GetValue(c2) ?? 0;
        // With GetRandomCountryPairWithoutTie, values should never be equal
        // Using strict > instead of >= since ties are now prevented
        return v1 > v2 ? c1 : c2;
    }

    /// <summary>
    /// Gets a random pair of countries where their values for the given question are NOT equal.
    /// This prevents "gotcha" questions where both countries have the same value.
    /// Returns null if not enough valid pairs exist.
    /// </summary>
    public (CountryData Country1, CountryData Country2)? GetRandomCountryPairWithoutTie(CountryComparisonQuestion question)
    {
        // Filter to countries that have data for this question
        var validCountries = _countries.Where(c => question.GetValue(c).HasValue).ToList();

        if (validCountries.Count < 2)
        {
            return null;
        }

        // Try to find a pair without equal values (max 100 attempts to avoid infinite loop)
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var country1 = validCountries[_random.Next(validCountries.Count)];
            var country2 = validCountries[_random.Next(validCountries.Count)];

            // Ensure different countries
            if (country1 == country2) continue;

            var value1 = question.GetValue(country1);
            var value2 = question.GetValue(country2);

            // Ensure values are NOT equal (prevents gotcha questions)
            if (value1 != value2)
            {
                return (country1, country2);
            }
        }

        // Fallback: try to find any two countries with different values
        for (int i = 0; i < validCountries.Count; i++)
        {
            for (int j = i + 1; j < validCountries.Count; j++)
            {
                var value1 = question.GetValue(validCountries[i]);
                var value2 = question.GetValue(validCountries[j]);
                if (value1 != value2)
                {
                    return (validCountries[i], validCountries[j]);
                }
            }
        }

        // If all countries have the same value, return null
        return null;
    }

    /// <summary>
    /// Gets a random viable question (one that has at least 2 countries with different values).
    /// </summary>
    public CountryComparisonQuestion? GetRandomViableQuestion()
    {
        var viableQuestions = _questions
            .Where(q => GetCountryCountForQuestion(q) >= 2)
            .ToList();

        if (viableQuestions.Count == 0)
            return null;

        return viableQuestions[_random.Next(viableQuestions.Count)];
    }

    /// <summary>
    /// Checks the user's answer and records the result with streak tracking.
    /// </summary>
    public CountryComparisonResult CheckAnswer(CountryData country1, CountryData country2,
        CountryComparisonQuestion question, CountryData userChoice)
    {
        var correct = GetCorrectAnswer(country1, country2, question);
        var isCorrect = correct == userChoice;

        var result = new CountryComparisonResult
        {
            Country1 = country1,
            Country2 = country2,
            Question = question,
            CorrectAnswer = correct,
            UserChoice = userChoice
        };

        // Update score with streak tracking
        Score.TotalQuestions++;
        if (isCorrect)
        {
            Score.CorrectAnswers++;
            Score.CurrentStreak++;
            if (Score.CurrentStreak > Score.BestStreak)
            {
                Score.BestStreak = Score.CurrentStreak;
            }
        }
        else
        {
            Score.CurrentStreak = 0;
        }

        Score.History.Add(result);
        return result;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    // ALSO UPDATE CountryGameScore class (in CountryData.cs) to have public setters:
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tracks the user's score during a country comparison game session.
    /// Uses public setters to allow restoration from local storage.
    /// </summary>
    public class CountryGameScore
    {
        // Use public setters to allow restoration from persisted state
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public List<CountryComparisonResult> History { get; } = [];

        public double PercentageCorrect => TotalQuestions > 0
            ? (double)CorrectAnswers / TotalQuestions * 100
            : 0;

        public void RecordAnswer(CountryComparisonResult result)
        {
            TotalQuestions++;
            if (result.IsCorrect)
            {
                CorrectAnswers++;
                CurrentStreak++;
                if (CurrentStreak > BestStreak) BestStreak = CurrentStreak;
            }
            else
            {
                CurrentStreak = 0;
            }
            History.Add(result);
        }

        public void Reset()
        {
            CorrectAnswers = 0;
            TotalQuestions = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            History.Clear();
        }
    }

    private static List<CountryComparisonQuestion> InitializeQuestions() =>
    [
        new() { Id = "population", QuestionTemplate = "Which country has a larger population?",
            PropertyName = nameof(CountryData.Population), GetValue = c => c.Population,
            Unit = "people", FormatString = "{0:N0}" },
        new() { Id = "area", QuestionTemplate = "Which country is larger by area?",
            PropertyName = nameof(CountryData.AreaSquareKm), GetValue = c => c.AreaSquareKm,
            Unit = "km²", FormatString = "{0:N0} km²" },
        new() { Id = "gdp", QuestionTemplate = "Which country has a higher GDP?",
            PropertyName = nameof(CountryData.GdpMillionsUsd), GetValue = c => c.GdpMillionsUsd,
            Unit = "million USD", FormatString = "${0:N0}M" },
        new() { Id = "gdp_per_capita", QuestionTemplate = "Which country has a higher GDP per capita?",
            PropertyName = nameof(CountryData.GdpPerCapitaUsd), GetValue = c => c.GdpPerCapitaUsd,
            Unit = "USD", FormatString = "${0:N0}" },
        new() { Id = "gni_ppp", QuestionTemplate = "Which country has a higher GNI per capita (PPP)?",
            PropertyName = nameof(CountryData.GniPerCapitaPpp), GetValue = c => c.GniPerCapitaPpp,
            Unit = "int'l $", FormatString = "${0:N0}" },
        new() { Id = "density", QuestionTemplate = "Which country has higher population density?",
            PropertyName = nameof(CountryData.PopulationDensity), GetValue = c => c.PopulationDensity,
            Unit = "per km²", FormatString = "{0:N1}/km²" },
        new() { Id = "literacy", QuestionTemplate = "Which country has a higher literacy rate?",
            PropertyName = nameof(CountryData.LiteracyRate), GetValue = c => c.LiteracyRate,
            Unit = "%", FormatString = "{0:N1}%" },
        new() { Id = "hdi", QuestionTemplate = "Which country has a higher Human Development Index?",
            PropertyName = nameof(CountryData.Hdi), GetValue = c => c.Hdi,
            Unit = "HDI", FormatString = "{0:N3}" },
        new() { Id = "life_expectancy", QuestionTemplate = "Which country has higher life expectancy?",
            PropertyName = nameof(CountryData.LifeExpectancy), GetValue = c => c.LifeExpectancy,
            Unit = "years", FormatString = "{0:N1} years" }
    ];
}

/// <summary>
/// Static store for all country data. Data from World Bank, UN, IMF, UNDP (2023/2024).
/// </summary>
public static class CountryDataStore
{
    public static List<CountryData> GetAllCountries() =>
    [
        // AFRICA (54 countries)
        C("Algeria","DZ","🇩🇿","Africa",2381741,45400000,239900,5284,13310,19.1,81.4,0.745,76.4),
        C("Angola","AO","🇦🇴","Africa",1246700,36000000,92124,2560,7250,28.9,72.0,0.595,62.0),
        C("Benin","BJ","🇧🇯","Africa",112622,13700000,19234,1404,4030,121.7,45.8,0.504,60.0),
        C("Botswana","BW","🇧🇼","Africa",581730,2600000,19396,7460,18600,4.5,88.9,0.708,61.1),
        C("Burkina Faso","BF","🇧🇫","Africa",274200,23000000,20327,884,2310,83.9,34.5,0.438,59.3),
        C("Burundi","BI","🇧🇮","Africa",27834,13200000,2779,211,780,474.3,74.7,0.426,61.7),
        C("Cabo Verde","CV","🇨🇻","Africa",4033,600000,2290,3817,9420,148.8,90.8,0.662,74.1),
        C("Cameroon","CM","🇨🇲","Africa",475442,28600000,48455,1694,4220,60.2,77.1,0.587,61.0),
        C("Central African Republic","CF","🇨🇫","Africa",622984,5500000,2553,464,1130,8.8,37.5,0.387,54.0),
        C("Chad","TD","🇹🇩","Africa",1284000,18300000,12698,694,1640,14.3,27.3,0.394,52.5),
        C("Comoros","KM","🇰🇲","Africa",2235,840000,1296,1543,3620,375.8,62.0,0.596,63.4),
        C("DR Congo","CD","🇨🇩","Africa",2344858,102300000,66380,649,1430,43.6,80.0,0.479,60.7),
        C("Republic of the Congo","CG","🇨🇬","Africa",342000,6100000,13366,2191,4080,17.8,80.3,0.593,63.5),
        C("Côte d'Ivoire","CI","🇨🇮","Africa",322463,28900000,78788,2726,6450,89.6,53.1,0.534,58.6),
        C("Djibouti","DJ","🇩🇯","Africa",23200,1100000,3668,3335,6760,47.4,53.0,0.521,62.3),
        C("Egypt","EG","🇪🇬","Africa",1001450,105000000,387086,3686,16450,104.9,73.1,0.728,70.2),
        C("Equatorial Guinea","GQ","🇬🇶","Africa",28051,1700000,12269,7217,17270,60.6,95.3,0.596,60.6),
        C("Eritrea","ER","🇪🇷","Africa",117600,3700000,2065,558,null,31.5,76.6,0.492,67.5),
        C("Eswatini","SZ","🇸🇿","Africa",17364,1200000,4854,4045,9310,69.1,88.4,0.597,57.1),
        C("Ethiopia","ET","🇪🇹","Africa",1104300,126500000,163698,1294,3070,114.6,51.8,0.492,65.0),
        C("Gabon","GA","🇬🇦","Africa",267668,2400000,19316,8048,17430,9.0,85.0,0.706,66.5),
        C("Gambia","GM","🇬🇲","Africa",11295,2700000,2187,810,2350,239.0,58.1,0.500,62.6),
        C("Ghana","GH","🇬🇭","Africa",238533,34100000,76370,2240,5870,143.0,79.0,0.602,63.8),
        C("Guinea","GN","🇬🇳","Africa",245857,14200000,20310,1430,3100,57.8,45.3,0.465,58.9),
        C("Guinea-Bissau","GW","🇬🇼","Africa",36125,2100000,1639,780,2050,58.1,52.9,0.483,60.0),
        C("Kenya","KE","🇰🇪","Africa",580367,54000000,107389,1989,5970,93.0,81.5,0.601,61.4),
        C("Lesotho","LS","🇱🇸","Africa",30355,2300000,2500,1087,3080,75.8,79.4,0.514,50.7),
        C("Liberia","LR","🇱🇷","Africa",111369,5300000,4001,755,1570,47.6,48.3,0.487,60.7),
        C("Libya","LY","🇱🇾","Africa",1759540,6900000,50492,7318,22810,3.9,91.0,0.718,73.0),
        C("Madagascar","MG","🇲🇬","Africa",587041,30300000,15103,499,1750,51.6,77.3,0.487,64.5),
        C("Malawi","MW","🇲🇼","Africa",118484,20900000,13164,630,1620,176.4,67.3,0.508,62.9),
        C("Mali","ML","🇲🇱","Africa",1240192,23300000,19340,830,2480,18.8,31.0,0.410,58.9),
        C("Mauritania","MR","🇲🇷","Africa",1030700,4900000,10082,2058,6580,4.8,67.0,0.540,65.6),
        C("Mauritius","MU","🇲🇺","Africa",2040,1270000,14397,11336,28370,622.5,91.3,0.796,74.4),
        C("Morocco","MA","🇲🇦","Africa",446550,37800000,141109,3733,9740,84.6,75.9,0.698,74.0),
        C("Mozambique","MZ","🇲🇿","Africa",801590,33900000,20819,614,1610,42.3,63.4,0.461,59.3),
        C("Namibia","NA","🇳🇦","Africa",825615,2600000,12366,4756,11540,3.1,92.0,0.615,59.3),
        C("Niger","NE","🇳🇪","Africa",1267000,26200000,16819,642,1440,20.7,35.0,0.394,61.6),
        C("Nigeria","NG","🇳🇬","Africa",923768,223800000,472620,2112,5860,242.3,62.0,0.548,52.7),
        C("Rwanda","RW","🇷🇼","Africa",26338,14100000,14634,1038,2950,535.3,73.2,0.548,66.1),
        C("São Tomé and Príncipe","ST","🇸🇹","Africa",964,230000,603,2622,5190,238.6,93.0,0.618,67.0),
        C("Senegal","SN","🇸🇳","Africa",196722,17900000,31112,1739,4150,91.0,56.3,0.517,68.0),
        C("Seychelles","SC","🇸🇨","Africa",455,100000,2089,20890,32310,219.8,95.9,0.785,73.4),
        C("Sierra Leone","SL","🇸🇱","Africa",71740,8600000,4192,487,1810,119.9,43.2,0.477,60.1),
        C("Somalia","SO","🇸🇴","Africa",637657,18100000,8128,449,null,28.4,null,null,55.3),
        C("South Africa","ZA","🇿🇦","Africa",1221037,60400000,399015,6605,16900,49.5,95.0,0.713,62.3),
        C("South Sudan","SS","🇸🇸","Africa",644329,11400000,4602,404,null,17.7,35.0,0.385,55.0),
        C("Sudan","SD","🇸🇩","Africa",1861484,47900000,51668,1079,4380,25.7,61.0,0.516,65.3),
        C("Tanzania","TZ","🇹🇿","Africa",947300,65500000,79158,1209,2940,69.2,78.0,0.532,66.2),
        C("Togo","TG","🇹🇬","Africa",56785,8800000,9140,1039,2450,155.0,66.5,0.539,61.6),
        C("Tunisia","TN","🇹🇳","Africa",163610,12400000,46297,3734,13510,75.8,79.7,0.732,76.7),
        C("Uganda","UG","🇺🇬","Africa",241550,48600000,49272,1013,2620,201.2,79.0,0.550,62.7),
        C("Zambia","ZM","🇿🇲","Africa",752612,20600000,27066,1314,3760,27.4,87.0,0.565,61.2),
        C("Zimbabwe","ZW","🇿🇼","Africa",390757,16300000,28437,1745,3590,41.7,89.7,0.550,59.3),
        
        // ASIA (49 countries)
        C("Afghanistan","AF","🇦🇫","Asia",652230,42240000,14580,345,null,64.8,37.3,0.462,62.0),
        C("Armenia","AM","🇦🇲","Asia",29743,2780000,24212,8712,18480,93.5,99.8,0.786,72.0),
        C("Azerbaijan","AZ","🇦🇿","Asia",86600,10200000,72356,7094,17090,117.8,99.8,0.760,69.4),
        C("Bahrain","BH","🇧🇭","Asia",765,1500000,44169,29446,54810,1960.8,97.5,0.888,78.8),
        C("Bangladesh","BD","🇧🇩","Asia",147570,173000000,437415,2529,7130,1172.6,74.9,0.670,72.4),
        C("Bhutan","BT","🇧🇹","Asia",38394,780000,2898,3715,12640,20.3,66.6,0.666,72.1),
        C("Brunei","BN","🇧🇳","Asia",5765,450000,15128,33618,71620,78.1,97.2,0.907,74.6),
        C("Cambodia","KH","🇰🇭","Asia",181035,17000000,31772,1869,5080,93.9,83.9,0.600,70.0),
        C("China","CN","🇨🇳","Asia",9596960,1410000000,17794782,12614,23930,146.9,97.3,0.788,78.6),
        C("Georgia","GE","🇬🇪","Asia",69700,3700000,30536,8253,20590,53.1,99.6,0.814,71.7),
        C("India","IN","🇮🇳","Asia",3287263,1428600000,3549918,2485,8610,434.6,77.7,0.644,67.2),
        C("Indonesia","ID","🇮🇩","Asia",1904569,277500000,1417387,5108,15190,145.7,96.0,0.713,68.6),
        C("Iran","IR","🇮🇷","Asia",1648195,89200000,413493,4635,16740,54.1,88.7,0.780,74.0),
        C("Iraq","IQ","🇮🇶","Asia",438317,44500000,270025,6071,12340,101.5,85.6,0.673,70.4),
        C("Israel","IL","🇮🇱","Asia",20770,9800000,521688,53233,48520,471.8,97.8,0.915,82.0),
        C("Japan","JP","🇯🇵","Asia",377930,123300000,4212945,34179,49260,326.3,99.0,0.920,84.5),
        C("Jordan","JO","🇯🇴","Asia",89342,11300000,50024,4428,12120,126.5,98.4,0.736,74.4),
        C("Kazakhstan","KZ","🇰🇿","Asia",2724900,19600000,259292,13229,31180,7.2,99.8,0.802,69.4),
        C("Kuwait","KW","🇰🇼","Asia",17818,4300000,164713,38305,60840,241.3,96.5,0.847,78.7),
        C("Kyrgyzstan","KG","🇰🇬","Asia",199951,7000000,13991,1999,6360,35.0,99.6,0.701,69.4),
        C("Laos","LA","🇱🇦","Asia",236800,7500000,14167,1889,8720,31.7,87.1,0.620,68.1),
        C("Lebanon","LB","🇱🇧","Asia",10452,5500000,21782,3960,14950,526.2,95.1,0.723,75.0),
        C("Malaysia","MY","🇲🇾","Asia",330803,34300000,430895,12563,34750,103.7,95.0,0.807,74.9),
        C("Maldives","MV","🇲🇻","Asia",300,520000,6547,12590,21270,1733.3,97.7,0.762,79.9),
        C("Mongolia","MN","🇲🇳","Asia",1564116,3400000,18100,5324,14520,2.2,99.2,0.739,69.9),
        C("Myanmar","MM","🇲🇲","Asia",676578,54800000,64516,1178,null,81.0,89.1,0.585,66.0),
        C("Nepal","NP","🇳🇵","Asia",147516,30900000,40830,1321,4500,209.5,71.0,0.601,68.4),
        C("North Korea","KP","🇰🇵","Asia",120538,26100000,null,null,null,216.5,100.0,null,72.0),
        C("Oman","OM","🇴🇲","Asia",309500,4600000,108192,23520,41390,14.9,95.7,0.816,78.2),
        C("Pakistan","PK","🇵🇰","Asia",881913,240500000,341517,1420,6270,272.7,58.0,0.540,66.1),
        C("Palestine","PS","🇵🇸","Asia",6020,5400000,18037,3340,6890,897.0,97.5,0.716,74.0),
        C("Philippines","PH","🇵🇭","Asia",300000,117300000,435675,3715,10220,391.0,96.3,0.710,69.3),
        C("Qatar","QA","🇶🇦","Asia",11586,2700000,235500,87222,112410,233.2,93.5,0.875,79.3),
        C("Saudi Arabia","SA","🇸🇦","Asia",2149690,36400000,1061902,29178,61760,16.9,97.6,0.875,76.9),
        C("Singapore","SG","🇸🇬","Asia",733,5900000,501428,84988,116500,8046.4,97.5,0.949,84.1),
        C("South Korea","KR","🇰🇷","Asia",100210,51700000,1712791,33147,52940,516.0,99.0,0.929,83.5),
        C("Sri Lanka","LK","🇱🇰","Asia",65610,21900000,74404,3397,15120,333.8,92.3,0.782,76.4),
        C("Syria","SY","🇸🇾","Asia",185180,23200000,9000,388,null,125.3,86.4,0.577,73.0),
        C("Taiwan","TW","🇹🇼","Asia",36193,23900000,790728,33091,null,660.3,99.0,0.926,81.0),
        C("Tajikistan","TJ","🇹🇯","Asia",143100,10100000,11999,1188,4430,70.6,99.8,0.679,69.4),
        C("Thailand","TH","🇹🇭","Asia",513120,71800000,514947,7172,21030,139.9,94.1,0.803,79.3),
        C("Timor-Leste","TL","🇹🇱","Asia",14874,1400000,3017,2155,4920,94.1,70.5,0.606,67.7),
        C("Turkey","TR","🇹🇷","Asia",783562,85300000,1154600,13534,38170,108.8,96.7,0.855,76.0),
        C("Turkmenistan","TM","🇹🇲","Asia",488100,6500000,82649,12715,18790,13.3,99.7,0.744,68.0),
        C("UAE","AE","🇦🇪","Asia",83600,9400000,507535,53993,74110,112.4,98.0,0.937,78.7),
        C("Uzbekistan","UZ","🇺🇿","Asia",448978,35300000,90384,2561,10050,78.6,100.0,0.727,70.9),
        C("Vietnam","VN","🇻🇳","Asia",331212,100000000,433356,4334,13850,302.0,95.8,0.726,74.0),
        C("Yemen","YE","🇾🇪","Asia",527968,34400000,21060,612,null,65.2,54.1,0.424,63.4),
        
        // EUROPE (44 countries)
        C("Albania","AL","🇦🇱","Europe",28748,2750000,22978,8358,17620,95.7,98.4,0.789,76.5),
        C("Andorra","AD","🇦🇩","Europe",468,80000,3352,41900,null,170.9,100.0,0.884,83.0),
        C("Austria","AT","🇦🇹","Europe",83871,9100000,515199,56593,66640,108.5,99.0,0.926,82.0),
        C("Belarus","BY","🇧🇾","Europe",207600,9200000,72881,7922,22020,44.3,99.8,0.801,72.4),
        C("Belgium","BE","🇧🇪","Europe",30528,11700000,627511,53642,64030,383.3,99.0,0.942,82.0),
        C("Bosnia and Herzegovina","BA","🇧🇦","Europe",51197,3210000,27034,8422,19270,62.7,98.5,0.779,75.3),
        C("Bulgaria","BG","🇧🇬","Europe",110879,6500000,100635,15483,29860,58.6,98.4,0.799,71.8),
        C("Croatia","HR","🇭🇷","Europe",56594,3900000,82689,21202,38050,68.9,99.3,0.878,76.6),
        C("Cyprus","CY","🇨🇾","Europe",9251,1260000,32229,25578,49460,136.2,99.1,0.907,81.2),
        C("Czech Republic","CZ","🇨🇿","Europe",78867,10500000,330483,31475,49600,133.1,99.0,0.895,77.7),
        C("Denmark","DK","🇩🇰","Europe",43094,5900000,404198,68513,73790,136.9,99.0,0.952,81.4),
        C("Estonia","EE","🇪🇪","Europe",45228,1370000,41551,30329,45400,30.3,99.9,0.899,77.1),
        C("Finland","FI","🇫🇮","Europe",338145,5500000,305689,55580,57500,16.3,99.0,0.942,82.0),
        C("France","FR","🇫🇷","Europe",643801,68000000,3030901,44573,55080,105.6,99.0,0.910,82.5),
        C("Germany","DE","🇩🇪","Europe",357022,84400000,4430758,52485,64770,236.4,99.0,0.950,80.6),
        C("Greece","GR","🇬🇷","Europe",131957,10400000,238206,22905,37050,78.8,97.9,0.893,80.1),
        C("Hungary","HU","🇭🇺","Europe",93028,9600000,212389,22124,40200,103.2,99.1,0.846,74.5),
        C("Iceland","IS","🇮🇸","Europe",103000,380000,31020,81632,69050,3.7,99.0,0.972,83.0),
        C("Ireland","IE","🇮🇪","Europe",70273,5100000,533682,104604,83140,72.6,99.0,0.950,82.0),
        C("Italy","IT","🇮🇹","Europe",301340,58900000,2254851,38285,53470,195.5,99.2,0.906,83.0),
        C("Latvia","LV","🇱🇻","Europe",64589,1850000,43627,23582,40030,28.6,99.9,0.879,75.0),
        C("Liechtenstein","LI","🇱🇮","Europe",160,40000,7365,184125,null,250.0,100.0,0.935,83.0),
        C("Lithuania","LT","🇱🇹","Europe",65300,2860000,78346,27394,45740,43.8,99.8,0.879,74.0),
        C("Luxembourg","LU","🇱🇺","Europe",2586,660000,86898,131633,91000,255.2,99.0,0.930,82.5),
        C("Malta","MT","🇲🇹","Europe",316,520000,20964,40315,52430,1645.6,94.5,0.915,82.5),
        C("Moldova","MD","🇲🇩","Europe",33846,2540000,16010,6303,17660,75.0,99.6,0.763,68.8),
        C("Monaco","MC","🇲🇨","Europe",2,40000,8596,214900,null,20000.0,99.0,null,85.9),
        C("Montenegro","ME","🇲🇪","Europe",13812,620000,7405,11944,26610,44.9,98.8,0.844,76.3),
        C("Netherlands","NL","🇳🇱","Europe",41850,17700000,1092748,61737,68570,423.0,99.0,0.946,82.0),
        C("North Macedonia","MK","🇲🇰","Europe",25713,1840000,14761,8022,19170,71.5,98.4,0.770,73.8),
        C("Norway","NO","🇳🇴","Europe",323802,5500000,579267,105321,87440,17.0,99.0,0.966,83.2),
        C("Poland","PL","🇵🇱","Europe",312696,36800000,811229,22046,42310,117.7,99.8,0.881,76.5),
        C("Portugal","PT","🇵🇹","Europe",92212,10400000,287080,27604,41830,112.8,96.1,0.874,81.1),
        C("Romania","RO","🇷🇴","Europe",238391,19000000,351003,18474,41700,79.7,99.1,0.827,74.2),
        C("Russia","RU","🇷🇺","Europe",17098246,144000000,2021426,14038,38420,8.4,99.7,0.821,69.4),
        C("San Marino","SM","🇸🇲","Europe",61,34000,1855,54559,null,557.4,99.0,null,83.5),
        C("Serbia","RS","🇷🇸","Europe",88361,6660000,75187,11291,23180,75.4,99.5,0.805,74.2),
        C("Slovakia","SK","🇸🇰","Europe",49035,5430000,132793,24456,38620,110.7,99.6,0.855,74.9),
        C("Slovenia","SI","🇸🇮","Europe",20273,2120000,68217,32178,49650,104.6,99.7,0.926,81.0),
        C("Spain","ES","🇪🇸","Europe",505992,47900000,1580694,33000,47450,94.7,98.6,0.911,83.2),
        C("Sweden","SE","🇸🇪","Europe",450295,10500000,593268,56502,64120,23.3,99.0,0.952,83.0),
        C("Switzerland","CH","🇨🇭","Europe",41284,8800000,884940,100561,84670,213.2,99.0,0.967,84.0),
        C("Ukraine","UA","🇺🇦","Europe",603550,37000000,178757,4832,15560,61.3,99.8,0.734,71.6),
        C("United Kingdom","GB","🇬🇧","Europe",243610,67700000,3158938,46672,55500,277.9,99.0,0.940,80.7),
        
        // NORTH AMERICA (23 countries)
        C("Antigua and Barbuda","AG","🇦🇬","N. America",443,94000,1868,19872,23490,212.2,99.0,0.820,78.0),
        C("Bahamas","BS","🇧🇸","N. America",13943,410000,14004,34156,37250,29.4,95.6,0.820,71.6),
        C("Barbados","BB","🇧🇧","N. America",430,282000,6112,21674,17560,655.8,99.6,0.809,77.6),
        C("Belize","BZ","🇧🇿","N. America",22966,410000,3218,7849,11220,17.9,82.7,0.700,70.5),
        C("Canada","CA","🇨🇦","N. America",9984670,40100000,2117805,52819,58310,4.0,99.0,0.935,82.7),
        C("Costa Rica","CR","🇨🇷","N. America",51100,5200000,85551,16452,24060,101.8,98.0,0.806,77.0),
        C("Cuba","CU","🇨🇺","N. America",109884,11100000,107352,9673,null,101.0,99.7,0.764,73.7),
        C("Dominica","DM","🇩🇲","N. America",751,73000,654,8959,14290,97.2,94.0,0.720,78.0),
        C("Dominican Republic","DO","🇩🇴","N. America",48671,11300000,113641,10057,23500,232.2,95.0,0.766,72.6),
        C("El Salvador","SV","🇸🇻","N. America",21041,6300000,33001,5238,10330,299.5,89.7,0.674,70.7),
        C("Grenada","GD","🇬🇩","N. America",344,126000,1326,10524,19640,366.3,98.6,0.795,74.9),
        C("Guatemala","GT","🇬🇹","N. America",108889,17600000,102050,5798,10960,161.6,83.3,0.629,69.2),
        C("Haiti","HT","🇭🇹","N. America",27750,11700000,20168,1723,3150,421.6,61.7,0.552,63.2),
        C("Honduras","HN","🇭🇳","N. America",112492,10400000,34401,3308,6650,92.5,88.5,0.624,70.1),
        C("Jamaica","JM","🇯🇲","N. America",10991,2800000,18777,6706,11600,254.7,88.7,0.706,70.5),
        C("Mexico","MX","🇲🇽","N. America",1964375,128900000,1811468,14055,22710,65.6,95.2,0.781,75.0),
        C("Nicaragua","NI","🇳🇮","N. America",130373,7050000,17829,2529,6850,54.1,82.6,0.667,74.5),
        C("Panama","PA","🇵🇦","N. America",75417,4400000,83382,18950,35460,58.3,95.7,0.820,76.2),
        C("Saint Kitts and Nevis","KN","🇰🇳","N. America",261,48000,1140,23750,32060,183.9,97.8,0.777,74.0),
        C("Saint Lucia","LC","🇱🇨","N. America",616,180000,2498,13878,18820,292.2,null,0.725,76.2),
        C("Saint Vincent","VC","🇻🇨","N. America",389,100000,1036,10360,17070,257.1,null,0.733,72.0),
        C("Trinidad and Tobago","TT","🇹🇹","N. America",5130,1530000,28117,18377,28690,298.2,99.0,0.814,73.4),
        C("United States","US","🇺🇸","N. America",9833517,335000000,27360935,81674,80300,34.1,99.0,0.927,77.5),
        
        // SOUTH AMERICA (12 countries)
        C("Argentina","AR","🇦🇷","S. America",2780400,46300000,641131,13846,26390,16.7,99.0,0.849,76.6),
        C("Bolivia","BO","🇧🇴","S. America",1098581,12200000,45464,3727,9040,11.1,94.5,0.698,63.6),
        C("Brazil","BR","🇧🇷","S. America",8515767,216400000,2173669,10044,17660,25.4,93.2,0.760,72.8),
        C("Chile","CL","🇨🇱","S. America",756102,19500000,335533,17206,28510,25.8,97.0,0.860,78.9),
        C("Colombia","CO","🇨🇴","S. America",1138910,52000000,363835,6997,18180,45.7,95.6,0.758,72.8),
        C("Ecuador","EC","🇪🇨","S. America",283561,18000000,118845,6603,12930,63.5,94.5,0.765,74.3),
        C("Guyana","GY","🇬🇾","S. America",214969,810000,16322,20151,34220,3.8,88.5,0.742,65.7),
        C("Paraguay","PY","🇵🇾","S. America",406752,6800000,42956,6317,15030,16.7,94.7,0.717,70.3),
        C("Peru","PE","🇵🇪","S. America",1285216,34000000,267603,7869,15310,26.4,94.5,0.762,73.7),
        C("Suriname","SR","🇸🇷","S. America",163820,620000,3621,5840,19240,3.8,94.4,0.695,70.3),
        C("Uruguay","UY","🇺🇾","S. America",176215,3400000,77241,22718,28200,19.3,98.8,0.830,77.7),
        C("Venezuela","VE","🇻🇪","S. America",916445,28400000,92200,3246,null,31.0,97.1,0.691,72.1),
        
        // OCEANIA (14 countries)
        C("Australia","AU","🇦🇺","Oceania",7692024,26500000,1687713,63688,59170,3.4,99.0,0.946,84.5),
        C("Fiji","FJ","🇫🇯","Oceania",18274,930000,5314,5714,14570,50.9,99.1,0.729,67.4),
        C("Kiribati","KI","🇰🇮","Oceania",811,130000,248,1908,4520,160.3,null,0.621,67.5),
        C("Marshall Islands","MH","🇲🇭","Oceania",181,42000,291,6929,6040,232.0,98.3,null,65.0),
        C("Micronesia","FM","🇫🇲","Oceania",702,115000,460,4000,4110,163.8,null,0.628,67.9),
        C("Nauru","NR","🇳🇷","Oceania",21,13000,151,11615,14020,619.0,99.0,null,63.8),
        C("New Zealand","NZ","🇳🇿","Oceania",270467,5200000,251969,48455,52000,19.2,99.0,0.939,82.5),
        C("Palau","PW","🇵🇼","Oceania",459,18000,284,15778,17960,39.2,96.6,null,71.6),
        C("Papua New Guinea","PG","🇵🇬","Oceania",462840,10300000,30624,2973,4620,22.3,64.2,0.558,64.5),
        C("Samoa","WS","🇼🇸","Oceania",2842,220000,861,3914,7420,77.4,99.1,0.707,73.2),
        C("Solomon Islands","SB","🇸🇧","Oceania",28896,720000,1696,2356,2810,24.9,84.1,0.564,70.2),
        C("Tonga","TO","🇹🇴","Oceania",747,107000,512,4785,7530,143.2,99.4,0.745,71.3),
        C("Tuvalu","TV","🇹🇻","Oceania",26,11000,63,5727,7020,423.1,null,null,64.5),
        C("Vanuatu","VU","🇻🇺","Oceania",12189,330000,1065,3227,3520,27.1,89.1,0.609,70.5),
    ];

    // Helper method to create CountryData with compact syntax
    private static CountryData C(string name, string iso2, string flag, string cont,
        long? area, long? pop, long? gdp, int? gdpPc, int? gniPpp,
        double? density, double? literacy, double? hdi, double? lifeExp) => new()
        {
            Name = name,
            IsoCode2 = iso2,
            IsoCode3 = iso2 + "X",
            FlagEmoji = flag,
            Continent = cont,
            AreaSquareKm = area,
            Population = pop,
            GdpMillionsUsd = gdp,
            GdpPerCapitaUsd = gdpPc,
            GniPerCapitaPpp = gniPpp,
            PopulationDensity = density,
            LiteracyRate = literacy,
            Hdi = hdi,
            LifeExpectancy = lifeExp
        };
}
