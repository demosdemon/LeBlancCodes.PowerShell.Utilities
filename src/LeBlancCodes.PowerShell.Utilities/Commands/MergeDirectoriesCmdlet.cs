using System.IO;
using System.Management.Automation;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Internal;

namespace LeBlancCodes.PowerShell.Utilities.Commands
{
    /// <summary>
    ///     Merge-Directories Cmdlet
    ///     Merges <see cref="SourceDirectory" /> into <see cref="DestinationDirectory" />
    /// </summary>
    [Cmdlet(VerbsData.Merge, "Directories", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(FileInfo), ParameterSetName = new[] {"PassThru"})]
    [PublicAPI]
    public class MergeDirectoriesCmdlet : Cmdlet
    {
        /// <summary>
        ///     The source directory
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [Alias("Input", "Source", "Path", "PSPath")]
        public string SourceDirectory { get; set; }

        /// <summary>
        ///     The output directory
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("Output", "Dest")]
        public string DestinationDirectory { get; set; }

        /// <summary>
        ///     Normally, `Merge-Directories` does not generate any output. Specify <see cref="PassThru" /> to generate
        ///     <see cref="FileInfo" /> objects for each file.
        /// </summary>
        [Parameter(ParameterSetName = "PassThru")]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        ///     Specify <see cref="DeleteSource" /> to delete the source directory after merging the files.
        /// </summary>
        [Parameter]
        public SwitchParameter DeleteSource { get; set; }

        /// <summary>
        ///     Normally, `Merge-Directories` performs a depth-first traversal through the <see cref="SourceDirectory" />. Specify
        ///     <see cref="BreadthFirst" /> to indicate a breadth-first traversal instead.
        /// </summary>
        [Parameter(HelpMessage = "Merge-Directories defaults to depth-first traversal, specify BreadthFirst for a as-named traversal instead.")]
        public SwitchParameter BreadthFirst { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            CreateDirectory(DestinationDirectory);
        }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            if (!Directory.Exists(SourceDirectory))
            {
                WriteError(Error.DirectoryNotFound(SourceDirectory));
                return;
            }

            try
            {
                WalkDirectory(SourceDirectory, string.Empty);
                if (DeleteSource)
                    DeleteDirectory(SourceDirectory);
            }
            catch (IOException exp)
            {
                WriteError(new ErrorRecord(exp, nameof(IOException), ErrorCategory.InvalidOperation, SourceDirectory));
            }
        }

        private bool CreateDirectory(string directory)
        {
            if (Directory.Exists(directory)) return true;
            var process = ShouldProcess(directory, "Create Directory");
            if (process)
                Directory.CreateDirectory(directory);
            return process;
        }

        private void WalkDirectory(string root, string subroot)
        {
            var directory = Path.Combine(root, subroot);
            var destination = Path.Combine(DestinationDirectory, subroot);

            if (!CreateDirectory(destination)) return;

            if (BreadthFirst)
            {
                foreach (var file in Directory.GetFiles(directory))
                    MergeFile(root, subroot, Path.GetFileName(file));
            }

            foreach (var dir in Directory.GetDirectories(directory))
            {
                var name = Path.GetFileName(dir) ?? string.Empty;
                WalkDirectory(root, Path.Combine(subroot, name));
            }

            if (BreadthFirst) return;

            foreach (var file in Directory.GetFiles(directory))
                MergeFile(root, subroot, Path.GetFileName(file));
        }

        private void MergeFile(string root, string subroot, string name)
        {
            var source = Path.Combine(root, subroot, name);
            var dest = Path.Combine(DestinationDirectory, subroot, name);

            if (Directory.Exists(dest))
            {
                if (!DeleteDirectory(dest))
                    return;
            }

            var prompt = false;

            if (File.Exists(dest))
            {
                prompt = true;
                if (!ShouldProcess(dest, "Overwrite file"))
                    return;
            }

            if (!prompt)
            {
                if (!ShouldProcess(dest, "Copy File"))
                    return;
            }

            File.Copy(source, dest, true);

            if (PassThru)
                WriteObject(new FileInfo(dest));
        }

        private bool DeleteDirectory(string directory)
        {
            if (!ShouldProcess(directory, "Recursively delete directory"))
                return false;

            Directory.Delete(directory, true);
            return true;
        }
    }
}
