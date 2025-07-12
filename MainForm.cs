using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FFMpegCore;

namespace VideoSplitterApp
{
    public partial class MainForm : Form
    {
        string ffmpegPath = Path.Combine(Application.StartupPath, "ffmpeg", "ffmpeg.exe");
        string inputFile = "";

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.mkv",
                Title = "اختر ملف الفيديو"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                inputFile = openFileDialog.FileName;
                txtInputFile.Text = inputFile;
            }
        }

        private void btnSplit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
            {
                MessageBox.Show("يرجى اختيار ملف فيديو صالح أولاً.");
                return;
            }

            //FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            //if (folderDialog.ShowDialog() != DialogResult.OK)
            //    return;

            string outputFolder = Path.Combine(Path.GetDirectoryName(inputFile) ?? throw new InvalidOperationException(),"split");
            Directory.CreateDirectory(outputFolder);
            string outputPattern = Path.Combine(outputFolder, "clip_%03d.mp4");

            string arguments = $"-i \"{inputFile}\"  -force_key_frames \"expr:gte(t,n_forced*5)\" -f segment -segment_time 5 -reset_timestamps 1 -c:v libx264 -c:a aac \"{outputPattern}\"";

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = ffmpegPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                MessageBox.Show("تم تقطيع الفيديو بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtSourceFolder.Text = dlg.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtDestinationFolder.Text = dlg.SelectedPath;
            }
        }

        private void btnCopyPortraitVideos_Click(object sender, EventArgs e)
        {
           
        }

        private bool IsPortraitVideo(string filePath, out int width, out int height)
        {
            width = height = 0;

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffprobe",
                        Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 \"{filePath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadLine();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    var parts = output.Split(',');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out width) &&
                        int.TryParse(parts[1], out height))
                    {
                        return height > width;
                    }
                }
            }
            catch (Exception ex)
            {
                lstResults.Items.Add($"Error: {Path.GetFileName(filePath)} - {ex.Message}");
            }

            return false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string source = txtSourceFolder.Text;
            string destination = txtDestinationFolder.Text;

            if (!Directory.Exists(source) || !Directory.Exists(destination))
            {
                MessageBox.Show("Please select valid source and destination folders.");
                return;
            }

            string[] videoFiles = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            lstResults.Items.Clear();

            foreach (string file in videoFiles)
            {
                if (IsPortraitVideo(file, out int width, out int height))
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(destination, fileName);
                    if (File.Exists(destPath))
                    {
                        destPath = Path.Combine(destination, Path.GetRandomFileName()+  fileName);
                    }
                    File.Copy(file, destPath, overwrite: true);
                    lstResults.Items.Add($"Copied: {fileName} ({width}x{height})");
                }
            }

            MessageBox.Show("Portrait video copy completed.");
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputFile) || !File.Exists(inputFile))
            {
                MessageBox.Show("يرجى اختيار ملف فيديو صالح أولاً.");
                return;
            }

            // --- CONFIGURATION ---
            string inputPath = inputFile; // Change this to your source video file
            string outputFolder = Path.Combine(Path.GetDirectoryName(inputFile) ?? throw new InvalidOperationException(), "Clips");
            string outputDirectory = outputFolder;
            double clipDurationSeconds = 8;
            // ---------------------
            string fileName = Path.GetFileName(inputPath);
            Console.WriteLine("Video Splitter using FFmpeg and C#");
            Console.WriteLine("-----------------------------------");

            // 1. Validate that the input file exists
            if (!File.Exists(inputPath))
            {
         
                Console.WriteLine($"Error: Input file not found at '{inputPath}'");
                Console.WriteLine("Please make sure the video file is in the same directory as the application, or provide a full path.");
                Console.ResetColor();
             
            }

            // 2. Create the output directory if it doesn't exist
            Directory.CreateDirectory(outputDirectory);

            try
            {
                // 3. Get video duration to calculate the number of clips
                Console.WriteLine("Analyzing video file to get duration...");
                var mediaInfo = await FFProbe.AnalyseAsync(inputPath);
                var totalDuration = mediaInfo.Duration;
                Console.WriteLine($"Video duration: {totalDuration}");

                int clipCounter = 0;
                TimeSpan startTime = TimeSpan.Zero;

                // 4. Loop through the video, creating a clip for each 5-second interval
                while (startTime < totalDuration)
                {
                    clipCounter++;
                    string outputFileName = $"{fileName}_{clipCounter}.mp4";
                    string outputPath = Path.Combine(outputDirectory, outputFileName);

                    Console.WriteLine($"\nProcessing clip #{clipCounter}...");
                    Console.WriteLine($"Start Time: {startTime}");
                    Console.WriteLine($"Output: {outputPath}");

                    // 5. Use FFMpegCore to perform the cut
                    await FFMpegArguments
                        .FromFileInput(inputPath)
                        .OutputToFile(outputPath, false, options => options
                            // Seek to the start time of the clip
                            .Seek(startTime.Add(TimeSpan.FromSeconds(clipDurationSeconds+0.1)))
                            // Set the duration of the clip
                            .WithDuration(TimeSpan.FromSeconds(clipDurationSeconds-1))

                            // --- FIX APPLIED HERE ---
                            // Instead of copying the video stream, we re-encode it slightly
                            // to ensure the clip starts with a keyframe.
                            // 'libx264' is a high-quality, standard H.264 encoder.
                            .WithVideoCodec("libx264")

                            // We can still copy the audio codec as it doesn't rely on keyframes.
                            .WithAudioCodec("copy")

                            // This crucial argument forces the first frame of the output to be a keyframe.
                            .WithCustomArgument("-force_key_frames \"expr:gte(t,0)\"")
                        )
                        .ProcessAsynchronously();

              
                    Console.WriteLine($"Successfully created {outputFileName}");
              

                    // 6. Update the start time for the next clip
                    startTime = startTime.Add(TimeSpan.FromSeconds(clipDurationSeconds));
                }

                Console.WriteLine("\n-----------------------------------");
                MessageBox.Show(
                    $"Video splitting complete!\nTotal clips created: {clipCounter}\nClips saved in folder: '{outputDirectory}'");
            
           
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                MessageBox.Show("An error occurred during video processing.");
               
            }
        }
    }
}
