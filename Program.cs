using Components.SFX.Tonklang;

public unsafe class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Engine.FinderEngine.Start();
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);

            Console.WriteLine(e.StackTrace);

            Console.WriteLine(e.Source);

            Console.WriteLine(e.InnerException);
        }
    }
}
