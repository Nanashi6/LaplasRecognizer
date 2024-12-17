using System.Drawing;
using Microsoft.Win32;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace TCP_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Client client;                 
        string curFile;
        TimeHelper timeHelper;
        
        public MainWindow()
        {
            InitializeComponent();
            timeHelper = new TimeHelper();
            ChooseButton.IsEnabled = false;
        }

        /// <summary>
        /// Работа с файловой системой для выбора изображения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileSystem(object sender, RoutedEventArgs e)
        {
            OpenFileDialog files = new OpenFileDialog();
            files.Filter = "Images (*.png; *.jpg)|*.png; *.jpg";
            
            if (files.ShowDialog() == true)
            {
                try
                {
                    Input.Source = new BitmapImage(new Uri(files.FileName));
                    SubmitLabel.Content = files.FileName;
                    curFile = files.FileName;
                    SubmitButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке файла: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Метод отправки изображения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SubmitImage(object sender, RoutedEventArgs e)
        {
            if(client != null)
            {
                SubmitButton.IsEnabled = false;
                byte[] bytes = File.ReadAllBytes(curFile);
                byte threading = 0;

                if (ComboBoxMulty.SelectedItem != null)
                {
                    if (ComboBoxMulty.SelectedIndex == 0) threading = 0;
                    else threading = 1;
                }
                timeHelper.Start();

                DataForClient data = await client.SendImage(bytes, threading);;
            
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(data.ImageData);
                image.EndInit();
            
                Output.Source = image;
            
                SSS.Content = $"Время обработки: {data.Time}";
                SSS.Content += $" Время с учетом отправки/приёма данных: {timeHelper.Stop()}";
            
                SubmitButton.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Клиент не подключен к серверу");
            }
        }

        private void CloseWindow(object sender, EventArgs e)
        {
            //client.Shutdown(SocketShutdown.Both);
            client.Disconnect();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new Client(new IPEndPoint(IPAddress.Parse(ipString.Text), Int32.Parse(portString.Text)));
                ChooseButton.IsEnabled = true;
            }            
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при подключениии: " + ex.Message);
            }
        }
    }
}