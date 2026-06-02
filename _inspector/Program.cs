using System.Reflection;

var dllPath = args[0];
try {
    var asm = Assembly.LoadFrom(dllPath);
    foreach (var t in asm.GetExportedTypes().Where(t => t.Name.ToLower().Contains("classicwindow") || t.Name.ToLower().Contains("window")))
        Console.WriteLine($"{t.FullName}  [public: {t.IsPublic}]");
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
