using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
double average = (5 + 4 + 4 + 4) / 4.0;

Print("average", average);
Print("average.ToString(\"F1\")", average.ToString("F1"));
Print("Math.Round(average, 1)", Math.Round(average, 1));
Print("Math.Round(.., AwayFromZero)", Math.Round((decimal)average, 1, MidpointRounding.AwayFromZero));

static void Print(string label, object value) => Console.WriteLine($"{label,-30}{value}");
