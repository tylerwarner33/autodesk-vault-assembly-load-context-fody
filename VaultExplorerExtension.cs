using Autodesk.Connectivity.Explorer.Extensibility;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace IsolatedVaultAddin;

/// <remarks>
///	Assembly attributes (ApiVersion, ExtensionId) are generated via MSBuild in the .csproj file.
/// </remarks>
public class VaultExplorerExtension : IExplorerExtension
{
	public void OnStartup(IApplication application) { }

	public void OnLogOn(IApplication application) { }

	public void OnLogOff(IApplication application) { }

	public void OnShutdown(IApplication application) { }

	public IEnumerable<CommandSite>? CommandSites()
	{
		CommandItem SerilogPackageVersionCommandItem = new("SerilogPackageVersionCommand", "Serilog Version") { };

		SerilogPackageVersionCommandItem.Execute += SerilogPackageVersionCommandHandler;

		CommandSite toolbarCommandSite = new("SerilogPackageVersionCommand.Toolbar", "Serilog Version Command Site")
		{
			Location = CommandSiteLocation.AdvancedToolbar,
			DeployAsPulldownMenu = false,
		};

		toolbarCommandSite.AddCommand(SerilogPackageVersionCommandItem);

		List<CommandSite> commandSites = [toolbarCommandSite];

		return commandSites;
	}

	public IEnumerable<CustomEntityHandler>? CustomEntityHandlers() => null;

	public IEnumerable<DetailPaneTab>? DetailTabs() => null;

#if VAULT_HAS_DOCK_PANELS
	public IEnumerable<DockPanel>? DockPanels() => null;
#endif

	public IEnumerable<string>? HiddenCommands() => null;

	void SerilogPackageVersionCommandHandler(object? sender, CommandItemEventArgs eventArgs)
	{
		try
		{
			IApplication vaultApplication = eventArgs.Context.Application;

			string title = $"{vaultApplication.Title}.{vaultApplication.Version.Minor}";

			AssemblyInspector inspector = new();
			string assemblyVersionInfo = inspector.GetSerilogVersionInfo();

			MessageBox.Show(assemblyVersionInfo, title);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString());
		}
	}
}

/// <summary>
///	Isolated class that handles assembly inspection logic.
///	This runs in a separate AssemblyLoadContext to demonstrate isolation.
/// </summary>
[Isolator]
internal class AssemblyInspector
{
	public static string GetAssemblyVersionInfo(string targetAssemblyName, Type targetAssemblyType)
	{
		try
		{
			var stringBuilder = new System.Text.StringBuilder();

			var addinAssembly = typeof(AssemblyInspector).Assembly;
#if NETCOREAPP
			var addinAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(addinAssembly);
			var addinAssemblyLoadContextName = addinAssemblyLoadContext?.Name ?? "<default>";
#else
         var addinAppDomain = AppDomain.CurrentDomain;
         var addinAppDomainName = addinAppDomain?.FriendlyName ?? "<default>";
#endif

			stringBuilder.AppendLine("****  Requesting Add-In Assembly  ****");
			AppendKeyValuePair(stringBuilder, "Name", addinAssembly.GetName().Name);
#if NETCOREAPP
			AppendKeyValuePair(stringBuilder, "AssemblyLoadContext", addinAssemblyLoadContextName);
#else
         AppendKeyValuePair(stringBuilder, "AppDomain", addinAppDomainName);
#endif
			AppendKeyValuePair(stringBuilder, "Version", addinAssembly.GetName().Version?.ToString());
			AppendKeyValuePair(stringBuilder, "Path", addinAssembly.Location);
			stringBuilder.AppendLine();

			// Assembly used for target type.
			var usedTargetAssembly = targetAssemblyType.Assembly;
#if NETCOREAPP
			var usedTargetAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(usedTargetAssembly);
			var usedTargetAssemblyLoadContextName = usedTargetAssemblyLoadContext?.Name ?? "<default>";
#else
         var usedTargetAppDomain = AppDomain.CurrentDomain;
         var usedTargetAppDomainName = addinAppDomain?.FriendlyName ?? "<default>";
#endif

			stringBuilder.AppendLine($"****  Target '{targetAssemblyName}' Assembly Actually Used  ****");
			AppendKeyValuePair(stringBuilder, "Name", usedTargetAssembly.GetName().Name);
#if NETCOREAPP
			AppendKeyValuePair(stringBuilder, "AssemblyLoadContext", usedTargetAssemblyLoadContextName);
#endif
			AppendKeyValuePair(stringBuilder, "Version", usedTargetAssembly.GetName().Version?.ToString());
			AppendKeyValuePair(stringBuilder, "Path", usedTargetAssembly.Location);
			stringBuilder.AppendLine();

			// All loaded target assemblies.
#if NETCOREAPP
			stringBuilder.AppendLine($"****  All Loaded \"{targetAssemblyName}\" Assemblies (by AssemblyLoadContext)  ****");
			var groups = AppDomain.CurrentDomain
				 .GetAssemblies()
				 .Where(assembly => string.Equals(assembly.GetName().Name, targetAssemblyName, StringComparison.Ordinal))
				 .Select(assembly => new { Context = AssemblyLoadContext.GetLoadContext(assembly), Assembly = assembly })
				 .GroupBy(x => x.Context?.Name ?? "<default>")
				 .OrderBy(group => group.Key, StringComparer.Ordinal);

			foreach (var group in groups)
			{
				AppendKeyValuePair(stringBuilder, "AssemblyLoadContext", group.Key);
				foreach (var assembly in group)
				{
					AppendKeyValuePair(stringBuilder, "Version", assembly.Assembly.GetName().Version?.ToString());
					AppendKeyValuePair(stringBuilder, "Path", assembly.Assembly.Location);
				}
				stringBuilder.AppendLine();
			}
#else
         stringBuilder.AppendLine($"****  All Loaded '{targetAssemblyName}' Assemblies  ****");
         var assemblies = AppDomain.CurrentDomain
               .GetAssemblies()
               .Where(assembly => string.Equals(assembly.GetName().Name, targetAssemblyName, StringComparison.Ordinal))
               .OrderBy(assembly => assembly.GetName().Version);

         foreach (var assembly in assemblies)
         {
               AppendKeyValuePair(stringBuilder, "Version", assembly.GetName().Version?.ToString());
               AppendKeyValuePair(stringBuilder, "Path", assembly.Location);
               stringBuilder.AppendLine();
         }
#endif

			return stringBuilder.ToString();
		}
		catch (Exception ex)
		{
			return @$"Error getting assembly version info for '{targetAssemblyName}' with type '{targetAssemblyType}'.
Exception Message: {ex.Message}
Exception Stack Trace: {ex.StackTrace}";
		}
	}

	private static void AppendKeyValuePair(System.Text.StringBuilder stringBuilder, string key, string? value)
		 => stringBuilder.Append($"     {key}").Append(": ").AppendLine(value ?? "<n/a>");

	/// <summary>
	///	Gets Serilog version info with the type resolved inside the isolated context.
	/// </summary>
	public string GetSerilogVersionInfo()
	{
		return GetAssemblyVersionInfo(nameof(Serilog), typeof(Serilog.Log));
	}
}