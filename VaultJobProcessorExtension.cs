using Autodesk.Connectivity.JobProcessor.Extensibility;

namespace IsolatedVaultAddin;

/// <remarks>
///	Assembly attributes (ApiVersion, ExtensionId) are generated via MSBuild in the .csproj file.
/// </remarks>
public class VaultJobProcessorExtension : IJobHandler
{
	public bool CanProcess(string jobType) => true;

	public JobOutcome Execute(IJobProcessorServices context, IJob job) => JobOutcome.Success;

	public void OnJobProcessorStartup(IJobProcessorServices context) { }

	public void OnJobProcessorShutdown(IJobProcessorServices context) { }

	public void OnJobProcessorWake(IJobProcessorServices context) { }

	public void OnJobProcessorSleep(IJobProcessorServices context) { }
}