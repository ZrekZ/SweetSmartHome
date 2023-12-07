﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Net.Mail;
using System.Net;
using System.Net.Mime;

namespace WindowsFormsApp56
{
    public partial class Form1 : Form
    {
        static string placeToSave;
        static string directoryToSave;
        private static MySqlConnection GetConnection()
        {
            string connString =
                "Server=" + Config.host +
                ";Database=" + Config.database +
                ";port=" + Config.port +
                ";User Id=" + Config.username +
                ";password=" + Config.password;
            MySqlConnection conn = new MySqlConnection(connString);
            return conn;
        }
        public static DataTable Query(string query)
        {
            var conn = GetConnection();
            MySqlCommand command = conn.CreateCommand();
            command.CommandText = query;
            DataTable dt = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter(command);

            try
            {
                conn.Open();
                adapter.Fill(dt);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                conn.Close();
            }

            return dt;
        }
        public static DataTable GetTableData(string tableName)
        {
            return Query($"select * from {tableName};"); //получить данные таблицы по названию
        }
        public static DataTable UseSQLQuery(string query)
        {
            return Query($"{query};"); //получить данные таблицы по названию
        }
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();

            placeToSave = UseSQLQuery("Select Place_To_Save_Path from  Place_To_Save where Place_To_Save_Id = 1").Rows[0].ItemArray.First().ToString();
            directoryToSave = UseSQLQuery("Select Place_To_Save_Directory from  Place_To_Save where Place_To_Save_Id = 1").Rows[0].ItemArray.First().ToString();
        }

        private async void btnEgit_Click(object sender, EventArgs e)
        { 
            string name = txtFaceName.Text;

            await Task.Run(() =>
            {

                if (!recognition.SaveTrainingData(pictureBox2.Image, name)) MessageBox.Show("Ошибка", "При получении профиля произошла непредвиденная ошибка.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    for (int i = 0; i < 10; i++)
                    {

                        Thread.Sleep(100);
                        lblEgitilenAdet.Text = (i + 1) + " количество профилей.";

                    }


                recognition = null;
                train = null;


            });
            //Task.Yield().GetAwaiter();

            //recognition = new BusinessRecognition(directoryToSave, placeToSave, "yuz.xml");
            //train = new Classifier_Train(directoryToSave, placeToSave, "yuz.xml");
  recognition = new BusinessRecognition("D://", "Faces", "yuz.xml");
            train = new Classifier_Train("D://", "Faces", "yuz.xml");
        }
        //BusinessRecognition recognition = new BusinessRecognition(directoryToSave, placeToSave, "yuz.xml");
        //Classifier_Train train = new Classifier_Train(directoryToSave, placeToSave, "yuz.xml");
        BusinessRecognition recognition = new BusinessRecognition("D://", "Faces", "yuz.xml");
        Classifier_Train train = new Classifier_Train("D://", "Faces", "yuz.xml");
        private void Form1_Load(object sender, EventArgs e)
        {
            Capture capture = new Capture();
            capture.Start();
            capture.ImageGrabbed += (a, b) =>
            {
                var image = capture.RetrieveBgrFrame();
                var grayimage = image.Convert<Gray, byte>();
                HaarCascade haaryuz = new HaarCascade("haarcascade_frontalface_alt2.xml");
                MCvAvgComp[][] Yuzler = grayimage.DetectHaarCascade(haaryuz, 1.2, 5, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(15, 15));

                MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_COMPLEX, 0.5, 0.5);
                foreach (MCvAvgComp yuz in Yuzler[0])
                {
                    var sadeyuz = grayimage.Copy(yuz.rect).Convert<Gray, byte>().Resize(100, 100, INTER.CV_INTER_CUBIC);
                    //Изображения должны быть одинакового размера. Поэтому изменение размера было выполнено с помощью Resize. В противном случае будет получена ошибка в строке 245 класса Classifier_Train.
                    pictureBox2.Image = sadeyuz.ToBitmap();
                    if (train != null)
                        if (train.IsTrained)
                        {
                            string name = train.Recognise(sadeyuz);
                            int match_value = (int)train.Get_Eigen_Distance;
                            image.Draw(name + " ", ref font, new Point(yuz.rect.X - 2, yuz.rect.Y - 2), new Bgr(Color.LightGreen));
                            GetUserMail(name);
                        }
                    image.Draw(yuz.rect, new Bgr(Color.Red), 2);
                }
                pictureBox1.Image = image.ToBitmap();
            };
        }
        protected void GetUserMail(string userName)
        {
            try
            {
                var email = UseSQLQuery($"select User_Email from Users Where User_FIO = '{userName}'").Rows[0].ItemArray.First().ToString();
                UserEmail.Text = email;
                SendMessage(email, userName);
            }
            catch (Exception)
            {

            }
            
        }
        public void SendMessage(string mailAddress, string photoName)
        {
            using (var smtps = new SmtpClient("smtp.gmail.com", 25))
            {
                MailAddress from = new MailAddress("max59.tyt@gmail.com");
                MailAddress to = new MailAddress(mailAddress);
                MailMessage m = new MailMessage(from, to);
                // тема письма
                m.Subject = "Фотография";
                // письмо представляет код html
                m.IsBodyHtml = true;
                //m.AlternateViews.Add(getEmbeddedImage($"D:\Faces\face_{photoName}_*.jpg"));
                m.AlternateViews.Add(getEmbeddedImage($"\"D\\Faces\\face_{photoName}_*.jpg\""));
                // адрес smtp-сервера и порт, с которого будем отправлять письмо
                //SmtpClient smtp = new SmtpClient("smtp.gmail.com", 25);
                // логин и пароль
                smtps.Credentials = new NetworkCredential("max59.tyt@gmail.com", "qpbz sznw cdlo gitt ");
                smtps.EnableSsl = true;
                smtps.Send(m);
            }
        }
        private AlternateView getEmbeddedImage(String filePath)
        {
            LinkedResource res = new LinkedResource(filePath);
            res.ContentId = Guid.NewGuid().ToString();
            string htmlBody = @"<img src='cid:" + res.ContentId + @"'/>";
            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(res);
            return alternateView;
        }

        private void btnEgitimSil_Click(object sender, EventArgs e)
        {
            recognition.DeleteTrains();
        }
    }
}