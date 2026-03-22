namespace TestPluginSafe;

public class SafePlugin
{
    public int Add(int a, int b) => a + b;
    public string Greet(string name) => "Hello, " + name;
    public double[] ComputeScores(int count)
    {
        var scores = new double[count];
        for (int i = 0; i < count; i++)
            scores[i] = i * 0.5;
        return scores;
    }
}
