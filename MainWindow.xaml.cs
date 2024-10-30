using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using Renci.SshNet;

namespace CD_4
{
    public partial class MainWindow : Window
    {
        private bool isPlaying = false;
        private DispatcherTimer timer;


        private const string serverIp = "192.168.0.26";         // 서버 IP 주소
        private const int port = 22;                          // SSH 기본 포트
        private const string username = "user";           // 서버 사용자명
        private const string password = "myzero1124!";           // 서버 비밀번호
        private const string remoteFilePath = "C:\\Users\\user\\Desktop\\zz.txt";

        public MainWindow()
        {
            InitializeComponent();
            QueryVideo.MediaEnded += QueryVideo_MediaEnded;

            // 타이머 설정: 슬라이더 업데이트를 위한
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
        }

        private SshClient client; // SshClient를 클래스 멤버로 선언하여 다른 메서드에서도 접근 가능하게 설정

private void ConnectToServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // SSH 연결 구성 설정
                var connectionInfo = new ConnectionInfo(serverIp, port, username,
                    new PasswordAuthenticationMethod(username, password))
                {
                    Timeout = TimeSpan.FromSeconds(30) // 타임아웃을 30초로 설정
                };

                client = new SshClient(connectionInfo); // 클래스 멤버로 선언된 client에 할당
                client.HostKeyReceived += (sender, e) =>
                {
                    e.CanTrust = true;
                };

                MessageBox.Show("서버에 연결을 시도합니다...");
                client.Connect();

                if (client.IsConnected)
                {
                    MessageBox.Show("서버에 성공적으로 연결되었습니다.");

                    // 파일 읽기 작업 수행
                    string fileContent = ReadRemoteFile(client, remoteFilePath);
                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        FileContentTextBox.Text = fileContent;
                    }
                    else
                    {
                        FileContentTextBox.Text = "파일 내용이 비어 있습니다.";
                    }

                    // client.Disconnect();를 호출하지 않아 연결을 유지
                }
                else
                {
                    MessageBox.Show("서버에 연결되지 않았습니다. 설정을 확인하세요.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SSH 연결 오류 발생: " + ex.Message);
            }
        }

        // 프로그램 종료 시 연결을 수동으로 종료할 수 있도록 별도 메서드 구현
        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
                MessageBox.Show("서버 연결이 종료되었습니다.");
            }
        }

        private string ReadRemoteFile(SshClient client, string path)
        {
            try
            {
                // CMD의 'type' 명령어를 사용하여 Windows 파일 내용 읽기
                var command = client.CreateCommand($"type \"{path}\"");
                var result = command.Execute();

                // 파일 내용 디버깅 메시지 추가
                MessageBox.Show("파일 내용: " + result);

                // 파일이 없거나 빈 경우 확인
                if (string.IsNullOrEmpty(result))
                {
                    throw new FileNotFoundException("파일을 찾을 수 없거나 파일이 비어 있습니다.");
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 읽기 중 오류 발생: " + ex.Message);
                return string.Empty;
            }
        }
        private void QueryVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
            QueryVideo.Stop();
            timer.Stop();
            VideoSlider.Value = 0;
        }

        // Select_Query 버튼 클릭 시 동영상 파일 선택
        private void SelectQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Video Files|*.mp4;*.avi;*.wmv;*.mov;*.mkv",
                    Title = "Select a Video File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // MediaElement의 Source 설정 및 재생
                    QueryVideo.Source = new Uri(openFileDialog.FileName);
                    QueryVideo.LoadedBehavior = MediaState.Manual;
                    QueryVideo.UnloadedBehavior = MediaState.Manual;
                    QueryVideo.Play();
                    isPlaying = true;
                    timer.Start();  // 슬라이더 업데이트 시작
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"동영상을 재생하는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // BW 5s 버튼 클릭 이벤트
        private void BackwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (QueryVideo.NaturalDuration.HasTimeSpan &&
                QueryVideo.Position.TotalSeconds > 5)
            {
                QueryVideo.Position -= TimeSpan.FromSeconds(5);
            }
            else
            {
                QueryVideo.Position = TimeSpan.Zero;
            }
        }

        // FW 5s 버튼 클릭 이벤트
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (QueryVideo.NaturalDuration.HasTimeSpan)
            {
                if (QueryVideo.Position.TotalSeconds + 5 <
                    QueryVideo.NaturalDuration.TimeSpan.TotalSeconds)
                {
                    QueryVideo.Position += TimeSpan.FromSeconds(5);
                }
                else
                {
                    QueryVideo.Position = QueryVideo.NaturalDuration.TimeSpan;
                }
            }
        }

        // TG 버튼 클릭 이벤트
        private void TGButton_Click(object sender, RoutedEventArgs e)
        {
            // 재생/일시정지 토글
            if (isPlaying)
            {
                QueryVideo.Pause();
                isPlaying = false;
                timer.Stop();
                TogglePlayImage.Source = new BitmapImage(new Uri("Resources/play.png", UriKind.Relative));
            }
            else
            {
                QueryVideo.Play();
                isPlaying = true;
                timer.Start();
                TogglePlayImage.Source = new BitmapImage(new Uri("Resources/pause.png", UriKind.Relative));
            }
        }

        // Screen_Shot 버튼 클릭 이벤트
        private void ScreenShotButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 동영상을 일시정지
                bool wasPlaying = isPlaying;
                QueryVideo.Pause();
                isPlaying = false;

                var dialog = new FolderBrowserDialogWPF
                {
                    Description = "이미지를 저장할 폴더를 선택하세요."
                };

                if (dialog.ShowDialog(this) == true)
                {
                    // 스크린샷을 저장하기 위해 MediaElement를 렌더링
                    RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                        (int)QueryVideo.ActualWidth, (int)QueryVideo.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTarget.Render(QueryVideo);

                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                    string filePath = Path.Combine(dialog.SelectedPath ?? string.Empty, "Query.jpg");
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    MessageBox.Show("이미지가 성공적으로 저장되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 저장이 완료된 후 동영상을 다시 재생
                if (wasPlaying)
                {
                    QueryVideo.Play();
                    isPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지를 저장하는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);

                // 오류 발생 시에도 동영상을 재생 상태로 되돌림
                if (!isPlaying)
                {
                    QueryVideo.Play();
                    isPlaying = true;
                }
            }
        }

        // 슬라이더 값 변경 이벤트: 동영상 위치 이동
        private void VideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoSlider.IsMouseCaptureWithin || VideoSlider.IsMoveToPointEnabled)
            {
                QueryVideo.Position = TimeSpan.FromSeconds(VideoSlider.Value);
            }
        }

        // 타이머 틱 이벤트: 슬라이더 업데이트 (현재 재생 시간에 맞춰 슬라이더를 이동)
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (QueryVideo.NaturalDuration.HasTimeSpan)
            {
                VideoSlider.Maximum = QueryVideo.NaturalDuration.TimeSpan.TotalSeconds;
                VideoSlider.Value = QueryVideo.Position.TotalSeconds;
            }
        }

        // WPF용 폴더 브라우저 대화상자 클래스
        public class FolderBrowserDialogWPF
        {
            public string? SelectedPath { get; set; }
            public string? Description { get; set; }

            public FolderBrowserDialogWPF()
            {
                SelectedPath = string.Empty;
                Description = string.Empty;
            }

            public bool? ShowDialog(Window owner = null)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "폴더를 선택하세요.",
                    Filter = "Folders|\n",
                    Title = Description,
                    ValidateNames = false
                };

                if (dlg.ShowDialog(owner) == true)
                {
                    SelectedPath = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
                    return true;
                }

                return false;
            }
        }


        

        // Select_Gallery 버튼 클릭 이벤트
        private void SelectGallery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // OpenFileDialog 설정
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Video Files|*.mp4;*.avi;*.wmv;*.mov;*.mkv",
                    Title = "Select Video Files",
                    Multiselect = true // 다중 선택 가능
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var selectedFiles = openFileDialog.FileNames;

                    // 5개 이상의 비디오를 선택하면 경고 메시지 출력
                    if (selectedFiles.Length > 4)
                    {
                        MessageBox.Show("4개 이하의 비디오만 선택하세요.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 선택한 비디오 파일들을 Video1 ~ Video4에 할당 및 재생
                    MediaElement[] videoElements = { Video1, Video2, Video3, Video4 };

                    // 선택한 파일 수에 따라 MediaElement에 비디오 할당
                    for (int i = 0; i < selectedFiles.Length; i++)
                    {
                        videoElements[i].Source = new Uri(selectedFiles[i], UriKind.Absolute);
                        videoElements[i].LoadedBehavior = MediaState.Manual;
                        videoElements[i].UnloadedBehavior = MediaState.Manual;
                        videoElements[i].Stretch = Stretch.Uniform;
                        videoElements[i].Play(); // 비디오 재생
                    }

                    // 선택되지 않은 나머지 Video는 초기화
                    for (int i = selectedFiles.Length; i < 4; i++)
                    {
                        videoElements[i].Source = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"비디오를 선택하는 중 오류가 발생했습니다: {ex.Message}",
                                "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 타이머, Query 관련 코드 생략 (위 코드 참조)
    }
}

