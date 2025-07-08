namespace BedrockAgentFunction;

public class AirportService
{
    private readonly Dictionary<string, AirportInfo> _airportsByCity = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "New York",
            new AirportInfo { City = "New York", Code = "JFK", Name = "John F. Kennedy International Airport" }
        },
        { "London", new AirportInfo { City = "London", Code = "LHR", Name = "London Heathrow Airport" } },
        { "Paris", new AirportInfo { City = "Paris", Code = "CDG", Name = "Charles de Gaulle Airport" } },
        { "Tokyo", new AirportInfo { City = "Tokyo", Code = "HND", Name = "Tokyo Haneda Airport" } },
        { "Sydney", new AirportInfo { City = "Sydney", Code = "SYD", Name = "Sydney Airport" } },
        {
            "Los Angeles",
            new AirportInfo { City = "Los Angeles", Code = "LAX", Name = "Los Angeles International Airport" }
        },
        { "Berlin", new AirportInfo { City = "Berlin", Code = "TXL", Name = "Berlin Tegel Airport" } },
        { "Dubai", new AirportInfo { City = "Dubai", Code = "DXB", Name = "Dubai International Airport" } },
        {
            "Toronto",
            new AirportInfo { City = "Toronto", Code = "YYZ", Name = "Toronto Pearson International Airport" }
        },
        { "Singapore", new AirportInfo { City = "Singapore", Code = "SIN", Name = "Singapore Changi Airport" } },
        { "Hong Kong", new AirportInfo { City = "Hong Kong", Code = "HKG", Name = "Hong Kong International Airport" } },
        { "Madrid", new AirportInfo { City = "Madrid", Code = "MAD", Name = "Adolfo Suárez Madrid–Barajas Airport" } },
        { "Rome", new AirportInfo { City = "Rome", Code = "FCO", Name = "Leonardo da Vinci International Airport" } },
        { "Moscow", new AirportInfo { City = "Moscow", Code = "SVO", Name = "Sheremetyevo International Airport" } },
        {
            "São Paulo",
            new AirportInfo
            {
                City = "São Paulo", Code = "GRU",
                Name = "São Paulo/Guarulhos–Governador André Franco Montoro International Airport"
            }
        },
        { "Istanbul", new AirportInfo { City = "Istanbul", Code = "IST", Name = "Istanbul Airport" } },
        { "Bangkok", new AirportInfo { City = "Bangkok", Code = "BKK", Name = "Suvarnabhumi Airport" } },
        {
            "Mexico City",
            new AirportInfo { City = "Mexico City", Code = "MEX", Name = "Mexico City International Airport" }
        },
        { "Cairo", new AirportInfo { City = "Cairo", Code = "CAI", Name = "Cairo International Airport" } },
        {
            "Buenos Aires",
            new AirportInfo { City = "Buenos Aires", Code = "EZE", Name = "Ministro Pistarini International Airport" }
        },
        {
            "Kuala Lumpur",
            new AirportInfo { City = "Kuala Lumpur", Code = "KUL", Name = "Kuala Lumpur International Airport" }
        },
        { "Amsterdam", new AirportInfo { City = "Amsterdam", Code = "AMS", Name = "Amsterdam Airport Schiphol" } },
        { "Barcelona", new AirportInfo { City = "Barcelona", Code = "BCN", Name = "Barcelona–El Prat Airport" } },
        { "Lima", new AirportInfo { City = "Lima", Code = "LIM", Name = "Jorge Chávez International Airport" } },
        { "Seoul", new AirportInfo { City = "Seoul", Code = "ICN", Name = "Incheon International Airport" } },
        {
            "Rio de Janeiro",
            new AirportInfo
            {
                City = "Rio de Janeiro", Code = "GIG",
                Name = "Rio de Janeiro/Galeão–Antonio Carlos Jobim International Airport"
            }
        },
        { "Dublin", new AirportInfo { City = "Dublin", Code = "DUB", Name = "Dublin Airport" } },
        { "Brussels", new AirportInfo { City = "Brussels", Code = "BRU", Name = "Brussels Airport" } },
        { "Lisbon", new AirportInfo { City = "Lisbon", Code = "LIS", Name = "Lisbon Portela Airport" } },
        { "Athens", new AirportInfo { City = "Athens", Code = "ATH", Name = "Athens International Airport" } },
        { "Oslo", new AirportInfo { City = "Oslo", Code = "OSL", Name = "Oslo Airport, Gardermoen" } },
        { "Stockholm", new AirportInfo { City = "Stockholm", Code = "ARN", Name = "Stockholm Arlanda Airport" } },
        { "Helsinki", new AirportInfo { City = "Helsinki", Code = "HEL", Name = "Helsinki-Vantaa Airport" } },
        { "Prague", new AirportInfo { City = "Prague", Code = "PRG", Name = "Václav Havel Airport Prague" } },
        { "Warsaw", new AirportInfo { City = "Warsaw", Code = "WAW", Name = "Warsaw Chopin Airport" } },
        { "Copenhagen", new AirportInfo { City = "Copenhagen", Code = "CPH", Name = "Copenhagen Airport" } },
        {
            "Budapest",
            new AirportInfo { City = "Budapest", Code = "BUD", Name = "Budapest Ferenc Liszt International Airport" }
        },
        { "Osaka", new AirportInfo { City = "Osaka", Code = "KIX", Name = "Kansai International Airport" } },
        {
            "San Francisco",
            new AirportInfo { City = "San Francisco", Code = "SFO", Name = "San Francisco International Airport" }
        },
        { "Miami", new AirportInfo { City = "Miami", Code = "MIA", Name = "Miami International Airport" } },
        {
            "Seattle", new AirportInfo { City = "Seattle", Code = "SEA", Name = "Seattle–Tacoma International Airport" }
        },
        { "Vancouver", new AirportInfo { City = "Vancouver", Code = "YVR", Name = "Vancouver International Airport" } },
        { "Melbourne", new AirportInfo { City = "Melbourne", Code = "MEL", Name = "Melbourne Airport" } },
        { "Auckland", new AirportInfo { City = "Auckland", Code = "AKL", Name = "Auckland Airport" } },
        { "Doha", new AirportInfo { City = "Doha", Code = "DOH", Name = "Hamad International Airport" } },
        {
            "Kuwait City", new AirportInfo { City = "Kuwait City", Code = "KWI", Name = "Kuwait International Airport" }
        },
        {
            "Bangalore", new AirportInfo { City = "Bangalore", Code = "BLR", Name = "Kempegowda International Airport" }
        },
        {
            "Beijing",
            new AirportInfo { City = "Beijing", Code = "PEK", Name = "Beijing Capital International Airport" }
        },
        {
            "Shanghai",
            new AirportInfo { City = "Shanghai", Code = "PVG", Name = "Shanghai Pudong International Airport" }
        },
        { "Manila", new AirportInfo { City = "Manila", Code = "MNL", Name = "Ninoy Aquino International Airport" } },
        {
            "Jakarta", new AirportInfo { City = "Jakarta", Code = "CGK", Name = "Soekarno–Hatta International Airport" }
        },
        {
            "Santiago",
            new AirportInfo
                { City = "Santiago", Code = "SCL", Name = "Comodoro Arturo Merino Benítez International Airport" }
        },
        { "Lagos", new AirportInfo { City = "Lagos", Code = "LOS", Name = "Murtala Muhammed International Airport" } },
        { "Nairobi", new AirportInfo { City = "Nairobi", Code = "NBO", Name = "Jomo Kenyatta International Airport" } },
        { "Chicago", new AirportInfo { City = "Chicago", Code = "ORD", Name = "O'Hare International Airport" } },
        {
            "Atlanta",
            new AirportInfo
                { City = "Atlanta", Code = "ATL", Name = "Hartsfield–Jackson Atlanta International Airport" }
        },
        {
            "Dallas",
            new AirportInfo { City = "Dallas", Code = "DFW", Name = "Dallas/Fort Worth International Airport" }
        },
        {
            "Washington, D.C.",
            new AirportInfo
                { City = "Washington, D.C.", Code = "IAD", Name = "Washington Dulles International Airport" }
        },
        { "Boston", new AirportInfo { City = "Boston", Code = "BOS", Name = "Logan International Airport" } },
        {
            "Philadelphia",
            new AirportInfo { City = "Philadelphia", Code = "PHL", Name = "Philadelphia International Airport" }
        },
        { "Orlando", new AirportInfo { City = "Orlando", Code = "MCO", Name = "Orlando International Airport" } },
        { "Denver", new AirportInfo { City = "Denver", Code = "DEN", Name = "Denver International Airport" } },
        {
            "Phoenix",
            new AirportInfo { City = "Phoenix", Code = "PHX", Name = "Phoenix Sky Harbor International Airport" }
        },
        { "Las Vegas", new AirportInfo { City = "Las Vegas", Code = "LAS", Name = "McCarran International Airport" } },
        {
            "Houston", new AirportInfo { City = "Houston", Code = "IAH", Name = "George Bush Intercontinental Airport" }
        },
        {
            "Detroit",
            new AirportInfo { City = "Detroit", Code = "DTW", Name = "Detroit Metropolitan Wayne County Airport" }
        },
        {
            "Charlotte",
            new AirportInfo { City = "Charlotte", Code = "CLT", Name = "Charlotte Douglas International Airport" }
        },
        {
            "Baltimore",
            new AirportInfo
            {
                City = "Baltimore", Code = "BWI", Name = "Baltimore/Washington International Thurgood Marshall Airport"
            }
        },
        {
            "Minneapolis",
            new AirportInfo
                { City = "Minneapolis", Code = "MSP", Name = "Minneapolis–Saint Paul International Airport" }
        },
        { "San Diego", new AirportInfo { City = "San Diego", Code = "SAN", Name = "San Diego International Airport" } },
        { "Portland", new AirportInfo { City = "Portland", Code = "PDX", Name = "Portland International Airport" } },
        {
            "Salt Lake City",
            new AirportInfo { City = "Salt Lake City", Code = "SLC", Name = "Salt Lake City International Airport" }
        },
        {
            "Cincinnati",
            new AirportInfo
                { City = "Cincinnati", Code = "CVG", Name = "Cincinnati/Northern Kentucky International Airport" }
        },
        {
            "St. Louis",
            new AirportInfo { City = "St. Louis", Code = "STL", Name = "St. Louis Lambert International Airport" }
        },
        {
            "Indianapolis",
            new AirportInfo { City = "Indianapolis", Code = "IND", Name = "Indianapolis International Airport" }
        },
        { "Tampa", new AirportInfo { City = "Tampa", Code = "TPA", Name = "Tampa International Airport" } },
        { "Milan", new AirportInfo { City = "Milan", Code = "MXP", Name = "Milan Malpensa Airport" } },
        { "Frankfurt", new AirportInfo { City = "Frankfurt", Code = "FRA", Name = "Frankfurt am Main Airport" } },
        { "Munich", new AirportInfo { City = "Munich", Code = "MUC", Name = "Munich Airport" } },
        {
            "Mumbai",
            new AirportInfo
                { City = "Mumbai", Code = "BOM", Name = "Chhatrapati Shivaji Maharaj International Airport" }
        },
        { "Cape Town", new AirportInfo { City = "Cape Town", Code = "CPT", Name = "Cape Town International Airport" } },
        { "Zurich", new AirportInfo { City = "Zurich", Code = "ZRH", Name = "Zurich Airport" } },
        { "Vienna", new AirportInfo { City = "Vienna", Code = "VIE", Name = "Vienna International Airport" } }
        // Add more airports as needed
    };

    public AirportInfo GetAirportInfoForCity(string city)
    {
        if (_airportsByCity.TryGetValue(city, out var airportInfo))
        {
            return airportInfo;
        }

        throw new KeyNotFoundException($"No airport information found for city: {city}");
    }
}

public class AirportInfo
{
    public string City { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Name} ({Code}) in {City}";
    }
}