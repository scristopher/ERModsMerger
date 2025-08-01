using ERModsMerger;
using ERModsMerger.Core;
using System.Text;

namespace ERModsMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string[] arguments = args;

            LOG.ConsoleOutput = true;

            ModsMergerConfig.LoadConfig();

            //save the loaded config, this add any new field when app is updated
            if (ModsMergerConfig.LoadedConfig != null)
                ModsMergerConfig.SaveConfig();


            if (arguments.Contains("/merge"))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Welcome to Elden Ring Mods Merger!\n");
                Console.ResetColor();

                ModsMerger.StartMerge(false, false);
                goto End;
            }

 

            DialConsole.WriteLine("Welcome to Elden Ring Mods Merger!\n", ConsoleColor.DarkYellow);

Start:

if (!File.Exists("ERModsMergerConfig\\config.json"))
{
    Thread.Sleep(300);
    DialConsole.Write("It's your first time launching me... ");
    Thread.Sleep(300);
    DialConsole.WriteLine("I see...\n");
    Thread.Sleep(300);
    DialConsole.WriteLine("Let me setup the basics for you, this should take only few seconds...\n");


    DialConsole.Write("And......... ");

    //FIRST LAUNCH INIT FOLDER, CONFIG AND FILES

    if (!Directory.Exists("ERModsMergerConfig"))
        Directory.CreateDirectory("ERModsMergerConfig");

    ModsMergerConfig config = new ModsMergerConfig();
    ModsMergerConfig.LoadedConfig = config;

    /*
    // search for elden ring path
    var pathEldenRing = Utils.GetInstallPath("ELDEN RING"); // return C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING
    if(pathEldenRing != null || pathEldenRing != "")
        ModsMergerConfig.LoadedConfig.GamePath = pathEldenRing + "\\Game";
    */

    ModsMergerConfig.SaveConfig();
    config = ModsMergerConfig.LoadConfig();

    config.CurrentProfile.ModsToMergeFolderPath = "ModsToMerge";
    config.CurrentProfile.MergedModsFolderPath = "MergedMods";

    Directory.CreateDirectory(config.CurrentProfile.ModsToMergeFolderPath);

    if(!Directory.Exists(config.CurrentProfile.MergedModsFolderPath))
        Directory.CreateDirectory(config.CurrentProfile.MergedModsFolderPath);

    //FIRST LAUNCH END INIT

    Thread.Sleep(300);
    DialConsole.WriteLine("VOILA!\n");


    Thread.Sleep(300);
    DialConsole.WriteLine("Now we're good to start the MERGE!\n\n");


    Thread.Sleep(500);
    DialConsole.WriteLine("CLEARING!");

    Thread.Sleep(700);
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("Welcome to Elden Ring Mods Merger!\n");
    Console.ResetColor();
    ModsMergerConfig.SaveConfig();
}

// config format error //reset it because the config should nnot be null at this point
if (ModsMergerConfig.LoadedConfig == null) 
{
    Console.WriteLine("🔴  Detected a format error in config.json, the file has been reset to default\n");
    ModsMergerConfig.LoadedConfig = new ModsMergerConfig();
    ModsMergerConfig.SaveConfig();
}

if (!File.Exists(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin"))
{
    Console.WriteLine("🔴  Invalid Game Path in ERModsMergerConfig\\config.json\n    " +
        "Please modify the configuration file and enter the correct path.\n    " +
        "Respect the format and don't forget to add double \\\\ between each folders.\n    " +
        "Save it and relaunch this tool.\n");

}


Thread.Sleep(300);
DialConsole.WriteLine("Let's get this done!\n");

Thread.Sleep(300);
DialConsole.Write("Copy your mods into the ");
DialConsole.Write("ModsToMerge", ConsoleColor.DarkYellow);
DialConsole.Write(" folder! ");

Thread.Sleep(500);
DialConsole.Write("Just try to respect the mod structure and I'll do my best.\n");
Thread.Sleep(500);
DialConsole.WriteLine("You want an example I guess...");

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n\n<<NO I'M READY, MERGE MY SH*T! (Press 'M')>>\t\t<<YES I WANT AN EXAMPLE (Press 'E')>>");
//Console.WriteLine("<<MERGE WITH MANUAL CONFLICTS RESOLVING (Press 'A')>>");
Console.ResetColor();

char keyPressed = Console.ReadKey(true).KeyChar;

//EXAMPLE
if(keyPressed == 'e' || keyPressed == 'E')
{
    Thread.Sleep(500);
    DialConsole.Write("\nOK then! Here is an example of how mods should be placed in the ModsToMerge folder:\n\n");

    Thread.Sleep(500);
    DialConsole.WriteLine("📂  ModsToMerge", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("   📂  ModExample1", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("      📄  regulation.bin", ConsoleColor.DarkGray);
    DialConsole.WriteLine("      📂  event", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("         📄  m10_00_00_00.emevd.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("         📄  m12_01_00_00.emevd.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("         📄  ...", ConsoleColor.DarkGray);
    DialConsole.WriteLine("      📂  parts", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("         📄  am_f_0000.partsbnd.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("         📄  wp_a_0424_l.partsbnd.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("         📄  ...\n", ConsoleColor.DarkGray);


    DialConsole.WriteLine("   📂  ModExample2", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("      📄  regulation.bin", ConsoleColor.DarkGray);
    DialConsole.WriteLine("      📂  map", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("         📂  mapstudio", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("            📄  m10_00_00_00.msb.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("            📄  m30_12_00_00.msb.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("            📄  ...", ConsoleColor.DarkGray);
    DialConsole.WriteLine("      📂  chr", ConsoleColor.DarkYellow);
    DialConsole.WriteLine("         📄  c0000_a00_lo.anibnd.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("         📄  c2010_div00.anibnd.dcx", ConsoleColor.DarkGray);
    DialConsole.WriteLine("         📄  ...", ConsoleColor.DarkGray);

    DialConsole.WriteLine("\nEtc etc etc...\n");

    Thread.Sleep(1000);

    DialConsole.Write("NOTES:\n- Mods have priority order depending of how they are placed in the");
    DialConsole.Write(" ModsToMerge ", ConsoleColor.DarkYellow);
    DialConsole.Write("folder (alphabetical order)\n  so be careful of the names you give to your mods folder.\n");
    DialConsole.WriteLine("- Conflicting regulation.bin will be merged into one. More merge possibilities will be added in the future.");
    DialConsole.WriteLine("- Conflicting other files (map, parts, etc) will keep the one with higher priority.\n");


    DisplayCurrentConfig();

    DialConsole.WriteLine("\nPress 'M' when you're ready for the merge!");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\n\n<<I'M READY, MERGE MY SH*T! (Press 'M')>>");
    Console.ResetColor();

    var kchar = Console.ReadKey(true).KeyChar;
    if (kchar == 'm' || kchar == 'M')
    {
        Thread.Sleep(500);
        DialConsole.WriteLine("\nLet's go! MERGE!\n\n");
        ModsMerger.StartMerge();
        DialConsole.WriteLine("\n\nIt's over, press any key to quit!");
        Console.ReadKey();
    }
    else
    {
        Fail();
        goto Start;
    }
}
//MERGE
else if(keyPressed == 'm'|| keyPressed == 'M')
{
    Thread.Sleep(500);
    DialConsole.WriteLine("\nLet's go! MERGE!\n\n");
    ModsMerger.StartMerge();
    DialConsole.WriteLine("\n\nIt's over, press any key to quit!");
    Console.ReadKey();
}
else if (keyPressed == 'a' || keyPressed == 'A')
{
    Thread.Sleep(500);
    DialConsole.WriteLine("\nLet's go! MANUAL MERGE!\n\n");
    ModsMerger.StartMerge(true);
    DialConsole.WriteLine("\n\nIt's over, press any key to quit!");
    Console.ReadKey();
}
            else
            {
                Fail();
                goto Start;
            }

            void DisplayCurrentConfig()
            {
                DialConsole.WriteLine("Current config:");
                DialConsole.WriteLine("Game Path:\t\t" + ModsMergerConfig.LoadedConfig.GamePath);
                DialConsole.WriteLine("Mods to be merged:\t" + ModsMergerConfig.LoadedConfig.CurrentProfile.ModsToMergeFolderPath);
                DialConsole.WriteLine("Merged mods:\t\t" + ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath);
                Console.WriteLine();

                DialConsole.Write("To modify the config, open and edit ");
                DialConsole.Write("ERModsMergerConfig\\Config.json", ConsoleColor.DarkYellow);
                DialConsole.Write(" with any text editor (eg: Notepad), remember to save it.\n");
            }

            void Fail()
            {
                DialConsole.WriteLine("\nBruh! For real? You just had to press one key and you FAILED!\n");
                Thread.Sleep(2500);
                DialConsole.WriteLine("Now you win the right to start all over again... cheh");
                Thread.Sleep(1000);
                Console.Clear();
                DialConsole.Write("Welcome to Elden Ring Mods Merger! ", ConsoleColor.DarkYellow);
                Thread.Sleep(1000);
                DialConsole.Write("I'm not even kidding.\n\n", ConsoleColor.DarkYellow);
            }

            End:
                ; // End of program
        }
    }
}

