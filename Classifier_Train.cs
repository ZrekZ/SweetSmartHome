using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace WindowsFormsApp56
{
    class Classifier_Train : IDisposable
    {
        string Dizin;
        string KlasorAdi;
        string XmlVeriDosyasi;
        public Classifier_Train(string Dizin, string KlasorAdi)
        {
            this.Dizin = Dizin + KlasorAdi;


            termCrit = new MCvTermCriteria(ContTrain, 0.001);
            _IsTrained = LoadTrainingData(this.Dizin);
        }
        public Classifier_Train(string Dizin, string KlasorAdi, string XmlVeriDosyasi)
        {
            this.Dizin = Dizin + KlasorAdi;
            this.XmlVeriDosyasi = XmlVeriDosyasi;


            termCrit = new MCvTermCriteria(ContTrain, 0.001);
            _IsTrained = LoadTrainingData(this.Dizin);
        }

        #region Переменные

        MCvTermCriteria termCrit;
        EigenObjectRecognizer recognizer;

        //Обучающие переменные
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();//Фото
        List<string> Names_List = new List<string>(); //Надписи
        int ContTrain, NumLabels;
        float Eigen_Distance = 0;
        string Eigen_label;
        int Eigen_threshold = 0;

        //Переменные класса
        string Error;
        bool _IsTrained = false;

        #endregion

        #region Конструкторы
        /// <summary>
        /// Default Constructor, Looks in (Dizin") for traing data.
        /// </summary>
        public Classifier_Train()
        {
            KlasorAdi = "TrainedFaces";
            Dizin = Application.StartupPath + "\\" + KlasorAdi;
            XmlVeriDosyasi = "TrainedLabels.xml";

            termCrit = new MCvTermCriteria(ContTrain, 0.001);
            _IsTrained = LoadTrainingData(Dizin);
        }

        /// <summary>
        /// Takes String input to a different location for training data
        /// </summary>
        /// <param name="Training_Folder"></param>
        public Classifier_Train(string Training_Folder)
        {
            termCrit = new MCvTermCriteria(ContTrain, 0.001);
            _IsTrained = LoadTrainingData(Training_Folder);
        }
        #endregion

        #region Public
        public bool IsTrained
        {
            get { return _IsTrained; }
        }

        /// <param name="Input_image"></param>
        /// <returns></returns>
        public string Recognise(Image<Gray, byte> Input_image, int Eigen_Thresh = -1)
        {
            if (_IsTrained)
            {
                EigenObjectRecognizer.RecognitionResult ER = recognizer.Recognize(Input_image);
                //Обрабатывать, если распознавание.EigenDistanceThreshold установлено как NULL
                if (ER == null)
                {
                    Eigen_label = "Tanımsız";
                    Eigen_Distance = 0;
                    return Eigen_label;
                }
                else
                {
                    Eigen_label = ER.Label;
                    Eigen_Distance = ER.Distance;
                    if (Eigen_Thresh > -1) Eigen_threshold = Eigen_Thresh;
                    if (Eigen_Distance > Eigen_threshold) return Eigen_label;
                    else return "Tanımsız";
                }

            }
            else return "";
        }


        public int Set_Eigen_Threshold
        {
            set
            {
                //recognizer.EigenDistanceThreshold = value;
                Eigen_threshold = value;
            }
        }

        public string Get_Eigen_Label
        {
            get
            {
                return Eigen_label;
            }
        }

        public float Get_Eigen_Distance
        {
            get
            {
                return Eigen_Distance;
            }
        }

        public string Get_Error
        {
            get { return Error; }
        }

        /// <param name="filename"></param>
        public void Save_Eigen_Recogniser(string filename)
        {
            StringBuilder sb = new StringBuilder();

            (new XmlSerializer(typeof(EigenObjectRecognizer))).Serialize(new StringWriter(sb), recognizer);
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(sb.ToString());
            xDoc.Save(filename);
        }

        /// <param name="filename"></param>
        public void Load_Eigen_Recogniser(string filename)
        {
            //Внедряем проверку ошибок
            FileStream EigenFS = File.OpenRead(filename);
            long Eflength = EigenFS.Length;
            byte[] xmlEBs = new byte[Eflength];
            EigenFS.Read(xmlEBs, 0, (int)Eflength);
            EigenFS.Close();

            MemoryStream xStream = new MemoryStream(xmlEBs);
            recognizer = (EigenObjectRecognizer)(new XmlSerializer(typeof(EigenObjectRecognizer))).Deserialize(xStream);
            _IsTrained = true;
            //_eigenImages[Array_location] = (Image<Gray, Single>)(new XmlSerializer(typeof(Image<Gray, Single>))).Deserialize(xStream);
        }

        public void Dispose()
        {
            recognizer = null;
            trainingImages = null;
            Names_List = null;
            Error = null;
            GC.Collect();
        }

        #endregion

        #region Private

        /// <param name="Folder_location"></param>
        /// <returns></returns>
        private bool LoadTrainingData(string Folder_location)
        {
            if (File.Exists(Folder_location + "\\" + XmlVeriDosyasi))
            {
                try
                {
                    Names_List.Clear();
                    trainingImages.Clear();
                    FileStream filestream = File.OpenRead(Folder_location + "\\" + XmlVeriDosyasi);
                    long filelength = filestream.Length;
                    byte[] xmlBytes = new byte[filelength];
                    filestream.Read(xmlBytes, 0, (int)filelength);
                    filestream.Close();

                    MemoryStream xmlStream = new MemoryStream(xmlBytes);

                    using (XmlReader xmlreader = XmlTextReader.Create(xmlStream))
                    {
                        while (xmlreader.Read())
                        {
                            if (xmlreader.IsStartElement())
                            {
                                switch (xmlreader.Name)
                                {
                                    case "NAME":
                                        if (xmlreader.Read())
                                        {
                                            Names_List.Add(xmlreader.Value.Trim());
                                            NumLabels += 1;
                                        }
                                        break;
                                    case "FILE":
                                        if (xmlreader.Read())
                                        {
                                            trainingImages.Add(new Image<Gray, byte>(Dizin + "\\" + xmlreader.Value.Trim()));
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    ContTrain = NumLabels;

                    if (trainingImages.ToArray().Length != 0)
                    {
                        //Распознаватель лиц
                        recognizer = new EigenObjectRecognizer(trainingImages.ToArray(),
                        Names_List.ToArray(), 5000, ref termCrit); //5000 default
                        return true;
                    }
                    else return false;
                }
                catch (Exception ex)
                {
                    Error = ex.ToString();
                    return false;
                }
            }
            else return false;
        }

        #endregion
    }
}
