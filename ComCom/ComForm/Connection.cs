using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;

namespace ComForm
{
    class Connection
    {
        int SuccessfulFrameNumber = 0;
        SerialPort _Port = new SerialPort();
        public SerialPort Port 
        {
            get
            {
                return _Port;
            }
            set
            {
                _Port = value;
                if (_Port.IsOpen)
                {
                    _Port.DiscardInBuffer();
                    _Port.DiscardOutBuffer();
                }
            }
        }

        public bool setPortName(string name)
        {
            string[] PortList = SerialPort.GetPortNames();

            if (Port.IsOpen)
            {
                Log.AppendText("port " + name + ": you can't change port name while it is opened\n");
                return false;
            }
            
            if (PortList.Contains(name))
            {
                Port.PortName = name;
                return true;
            }
            Log.AppendText("port " + name + " not found\n");  //нет такого порта
            return false;
        }

        public bool OpenPort()
        {
            try
            {
                Port.Open();
                Port.DtrEnable = true;
                InitializeHandlers();

                return true;
            }
            
            catch (System.IO.IOException) 
            {
                Log.AppendText("port " + Port.PortName + " not found\n");
                return false;
            }

            catch (System.InvalidOperationException) //открыт в этом приложении
            {
                Log.AppendText("port " + Port.PortName + "  is already opened\n");
                return false;
            }

            catch (System.UnauthorizedAccessException) //уже открыт в другом приложении/другим окном
            {
                Log.AppendText("port " + Port.PortName + "  is already used\n");
                return false;
            }
        }

        public bool ClosePort()
        {
            if (!Port.IsOpen)
            {
                Log.AppendText("fail: port is already closed\n");
                return false;
            }
            Port.Close();
            return true;
        }

        public bool IsConnected() //оба порта открыты и готовы слать данные
        {
            return Port.IsOpen && Port.DsrHolding;
        }



        //==================================================*/*/*/*

        public const byte STARTBYTE = 0xFF;

        const int HeaderLenght = 2;
        const int fileTypeLenght = 1;
        const int sizeLenght = 10;
        const int NumOfFrameLenght = 7;

        const int InfoLen = HeaderLenght + fileTypeLenght + sizeLenght + NumOfFrameLenght + NumOfFrameLenght;


        public enum FrameType : byte
        {
            ACK,
            MSG,
            RET_MSG,
            ERR_FILE,
            FILE,
            FRAME,
            FILEOK,
        }

        public static String FilePath;
        public bool BreakConnection = false;
        public void WriteData(string input, FrameType type)
        {
            byte[] Header = { STARTBYTE, (byte)type };

            byte[] fileId = { 0 };
            byte[] size;
            byte[] NumOfFrames;
            byte[] FrameNumber;
            
            byte[] BufferToSend;
            byte[] Telegram;
            string Telegram_s;
            string size_s;
            byte[] ByteToEncode;
            byte[] ByteEncoded;


            switch (type)
            {
                case FrameType.ERR_FILE:


                    break;
                case FrameType.MSG:

                    #region MSG
                    if (IsConnected())
                    {
                        // Telegram[] = Coding(input); 
                        Telegram = Encoding.Default.GetBytes(input); //потом это кыш

                        BufferToSend = new byte[Header.Length + Telegram.Length]; //буфер для отправки = заголовок+сообщение
                        Header.CopyTo(BufferToSend, 0);
                        Telegram.CopyTo(BufferToSend, Header.Length);

                        Port.Write(BufferToSend, 0, BufferToSend.Length);
                        Log.AppendText("(" + Port.PortName + ") WriteData: sent message >  " + Encoding.Default.GetString(Telegram) + "\n");
                    }
                    break;
                #endregion
                case FrameType.ACK:
                    #region ACK
                    if (IsConnected())
                    {
                        // Telegram[] = Coding(input);
                        Telegram = Encoding.Default.GetBytes(input); //потом это кыш

                        BufferToSend = new byte[Header.Length + Telegram.Length]; //буфер для отправки = заголовок+сообщение
                        Header.CopyTo(BufferToSend, 0);
                        Telegram.CopyTo(BufferToSend, Header.Length);

                        Port.Write(BufferToSend, 0, BufferToSend.Length);
                        Telegram_s = Encoding.Default.GetString(Telegram);
                        //Log.AppendText("[" + DateTime.Now + "] Отправлено ACK согласие на принятие файла: " + Telegram_s + "\n");
                    }
                    break;
                #endregion
                case FrameType.FILEOK:
                    if (IsConnected())
                    {
                        ByteToEncode = File.ReadAllBytes(input);
                        FilePath = input;
                        size = new byte[sizeLenght];
                        size = Encoding.Default.GetBytes(((double)ByteToEncode.Length).ToString()); //нужны байты
                        //Telegram = Encoding.Default.GetBytes(size); //потом это кыш

                        BufferToSend = new byte[Header.Length + size.Length]; //буфер для отправки = заголовок+сообщение
                        Header.CopyTo(BufferToSend, 0);
                        size.CopyTo(BufferToSend, Header.Length);

                        Port.Write(BufferToSend, 0, BufferToSend.Length);
                        size_s = Encoding.Default.GetString(size);
                        Log.AppendText("[" + DateTime.Now + "] Отправлена информация о размере файла: " + size_s + "\n");
                        //SuccessfulFrameNumber = int.Parse(Telegram_s);
                    }
                    break;
                case FrameType.FRAME:
                    if (IsConnected())
                    {
                        // Telegram[] = Coding(input); 
                        Telegram = Encoding.Default.GetBytes(input); //потом это кыш
                        BufferToSend = new byte[Header.Length + Telegram.Length]; //буфер для отправки = заголовок+сообщение
                        Header.CopyTo(BufferToSend, 0);
                        Telegram.CopyTo(BufferToSend, Header.Length);

                        Port.Write(BufferToSend, 0, BufferToSend.Length);
                        Telegram_s = Encoding.Default.GetString(Telegram);
                        //Log.AppendText("[" + DateTime.Now + "] получен Frame >  " + Telegram_s + " .Отправлено подтверждение о получении.\n");
                        //SuccessfulFrameNumber = int.Parse(Telegram_s);
                    }
                    else
                    {
                        Log.Invoke(new EventHandler(delegate
                        {
                            Log.AppendText("[" + DateTime.Now + "] : Передача файла нарушена.\n" + "Последний успешный фрейм: " + SuccessfulFrameNumber.ToString());
                        }));
                        MessageBox.Show("Соединение прервано. Передача нарушена.");
                        BreakConnection = true;
                        break;
                    }
                    break;

                case FrameType.FILE:
                    #region FILE
                    int i;
                    int parts = 0;
                    int EncodedByteIndex;
                    int Part_ByteEncodedIndex;
                    ByteEncoded = new byte[0];
                    size = new byte[0];
                    NumOfFrames = new byte[0];
                    
                   
                    if (IsConnected())
                    {
                        if (BreakConnection == false)
                        {
                            ByteToEncode = File.ReadAllBytes(@FilePath);

                            size = new byte[sizeLenght];
                            size = Encoding.Default.GetBytes(((double)ByteToEncode.Length).ToString()); //нужны байты
                            //WriteData(Encoding.Default.GetString(size), FrameType.FILEOK);
                            NumOfFrames = new byte[NumOfFrameLenght];
                            FrameNumber = new byte[NumOfFrameLenght];

                            string typeFile = @input.Split('.')[1];
                            fileId[0] = TypeFile_to_IdFile(typeFile);


                            ByteEncoded = new byte[ByteToEncode.Length * 2];
                            for (i = 0; i < ByteToEncode.Length; i++)
                            {
                                Hamming.HammingEncode74(ByteToEncode[i]).CopyTo(ByteEncoded, i * 2);
                            }

                            if (ByteEncoded.Length + InfoLen < Port.WriteBufferSize)
                            {
                                BufferToSend = new byte[InfoLen + ByteEncoded.Length];
                                Header.CopyTo(BufferToSend, 0);
                                fileId.CopyTo(BufferToSend, Header.Length);
                                size.CopyTo(BufferToSend, Header.Length + fileId.Length);

                                NumOfFrames = Encoding.Default.GetBytes(1.ToString());
                                NumOfFrames.CopyTo(BufferToSend, Header.Length + fileId.Length + sizeLenght);

                                FrameNumber = Encoding.Default.GetBytes(1.ToString());
                                FrameNumber.CopyTo(BufferToSend, Header.Length + fileId.Length + sizeLenght + NumOfFrameLenght);


                                ByteEncoded.CopyTo(BufferToSend, InfoLen);
                                bool flag = false;
                                while (!flag)
                                {

                                    if (MessageBox.Show("Отправить?", "Файл", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                    {

                                        flag = true;
                                        Port.Write(BufferToSend, 0, BufferToSend.Length);

                                        //loading.Hide();
                                        MessageBox.Show("Готово!");
                                        //loading.progressBar1.Value = 0;
                                        //loading.i = 1;

                                    }
                                    else
                                    {
                                        flag = true;
                                        //loading.Hide();
                                        //loading.progressBar1.Value = 0;
                                        MessageBox.Show("Вы отменили передачу файла.");
                                        // loading.i = 1;
                                    }
                                }
                            }
                            else
                            {
                                parts = (int)Math.Ceiling((double)ByteEncoded.Length / (double)(Port.WriteBufferSize - InfoLen));
                                NumOfFrames = Encoding.Default.GetBytes(parts.ToString());
                            }
                            SuccessfulFrameNumber = 0;
                        }

                        for (i = SuccessfulFrameNumber; i < parts; i++)
                        {
                            BreakConnection = false;

                            EncodedByteIndex = i * (Port.WriteBufferSize - InfoLen);
                            Part_ByteEncodedIndex = (Port.WriteBufferSize - InfoLen);

                            byte[] Part_ByteEncoded = new byte[Part_ByteEncodedIndex];

                            int Part_Len = 0;
                            if (((ByteEncoded.Length - EncodedByteIndex) >= Part_ByteEncodedIndex))
                            {
                                Part_Len = Part_ByteEncodedIndex;
                            }

                            else if (ByteEncoded.Length - EncodedByteIndex > 0)
                            {
                                Part_Len = ByteEncoded.Length - i * (Port.WriteBufferSize - InfoLen);
                            }

                            BufferToSend = new byte[Port.WriteBufferSize];

                            Header.CopyTo(BufferToSend, 0);
                            fileId.CopyTo(BufferToSend, Header.Length);
                            size.CopyTo(BufferToSend, Header.Length + fileId.Length);

                            NumOfFrames.CopyTo(BufferToSend, Header.Length + fileId.Length + sizeLenght);

                            FrameNumber = Encoding.Default.GetBytes((i + 1).ToString());
                            FrameNumber.CopyTo(BufferToSend, Header.Length + fileId.Length + sizeLenght + NumOfFrameLenght);

                            Array.ConstrainedCopy(ByteEncoded, EncodedByteIndex, BufferToSend, InfoLen, Part_Len);

                            //Log.AppendText("[" + DateTime.Now + "] : Отправляется фрейм: " + (SuccessfulFrameNumber + 1).ToString() + "\n");

                            Port.Write(BufferToSend, 0, BufferToSend.Length);
                            int ByteCheck;

                            if (i > 0)
                            {

                                int WaitTime = 0;
                                ByteCheck = Port.ReadByte();
                                while (ByteCheck != (int)STARTBYTE)
                                {
                                    if (WaitTime <= 1000)
                                    {
                                        Thread.Sleep(100);
                                        WaitTime += 100;
                                        ByteCheck = Port.ReadByte();
                                    }
                                }
                                ByteCheck = Port.ReadByte();
                                if (ByteCheck == (int)FrameType.FRAME)
                                {
                                    int n = FrameNumber.Length;//Port.BytesToRead;
                                    byte[] msgByteBuffer = new byte[n];

                                    Port.Read(msgByteBuffer, 0, n); //считываем сообщение
                                    string Message = Encoding.Default.GetString(msgByteBuffer);
                                    Log.Invoke(new EventHandler(delegate
                                    {
                                        Log.AppendText("[" + DateTime.Now + "] : Получено подтверждение об успешной доставке Frame " + Message + "\n");
                                    }));
                                    SuccessfulFrameNumber = int.Parse(Message);
                                }

                                if (i == SuccessfulFrameNumber)
                                {
                                    continue;
                                }


                                if (i != SuccessfulFrameNumber)
                                {
                                    MessageBox.Show("Передача файла прервана");
                                    BreakConnection = true;
                                    
                                }
                            }
                            if (BreakConnection == true)
                            {
                                MessageBox.Show("Восстановите соединение или отмените отправку файла", "Файл", MessageBoxButtons.OKCancel);

                                while ((IsConnected()) || (MessageBox.Show("Отправить?", "Файл", MessageBoxButtons.OKCancel) == DialogResult.Cancel))
                                {
                                    MessageBox.Show("Восстановите соединение или отмените отправку файла", "Файл", MessageBoxButtons.OKCancel);
                                }

                                if (MessageBox.Show("Отправить?", "Файл", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Log.Invoke(new EventHandler(delegate
                        {
                            Log.AppendText("[" + DateTime.Now + "] : Передача файла нарушена.\n" + "Последний успешный фрейм: "+SuccessfulFrameNumber.ToString());
                        }));
                        MessageBox.Show("Соединение прервано. Передача нарушена.");
                        BreakConnection = true;
                        break;
                    }
                    break;
                #endregion

                default:
                    if (IsConnected())
                        Port.Write(Header, 0, Header.Length);
                    break;
            }                                                                                                                                  //Зачем такая конструкция?
            Log.Invoke(new EventHandler(delegate
            {
                Log.AppendText("sent frame " + type + "\n"); //всё записываем, мы же снобы
            }));
        }


        public void InitializeHandlers()
        {
            Port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);
        }


        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Port.ReadByte() == STARTBYTE)
            {
                GetData(Port.ReadByte());
            }
        }


        byte[] file_buffer;

        public void GetData(int typeId)
        {
            FrameType type = (FrameType)typeId;

            byte[] ToDecode;
            byte[] Decoded;

            /*Log.Invoke(new EventHandler(delegate
            {
                Log.AppendText("get frame " + type +"\n");
            }));*/


            switch (type)
            {
                case FrameType.MSG:
                    #region MSG
                    if (IsConnected())
                    {
                        int n = Port.BytesToRead;
                        byte[] msgByteBuffer = new byte[n];
                        
                        Port.Read(msgByteBuffer, 0, n); //считываем сообщение
                        string Message = Encoding.Default.GetString(msgByteBuffer);
                        Log.Invoke(new EventHandler(delegate
                        {
                            Log.AppendText("(" + Port.PortName + ") GetData: new message > " + Message + "\n");
                        }));

                        WriteData(null, FrameType.ACK);
                    }
                    else
                    {
                        WriteData(null, FrameType.RET_MSG);
                    }
                    break;
                #endregion
                case FrameType.FILEOK:
                    #region FILEOK
                    if (IsConnected())
                    {
                        int n = Port.BytesToRead;
                        byte[] msgByteBuffer = new byte[n];

                        Port.Read(msgByteBuffer, 0, n); //считываем сообщение
                        string Message = Encoding.Default.GetString(msgByteBuffer);
                        Log.Invoke(new EventHandler(delegate
                        {
                            Log.AppendText("[" + DateTime.Now + "] : Получено предложение на прием файла размером: " + Message + "\n");
                        }));
                        //SuccessfulFrameNumber = int.Parse(Message);
                        int Message_num = int.Parse(Message);
                        double fileSize = Math.Round((double)Message_num / 1024, 3);
                        if (MessageBox.Show("Файл. Размер: " + fileSize.ToString() + " Кбайт.\nПринять?", "Прием файла", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            WriteData("OK", FrameType.ACK);
                           
                        }
                       
                    }
                    else
                    {
                        WriteData(null, FrameType.RET_MSG);
                    }
                    break;
                #endregion
                case FrameType.FRAME:
                    #region FRAME
                    //if (IsConnected())
                    //{
                    //    int n = Port.BytesToRead;
                    //    byte[] msgByteBuffer = new byte[n];

                    //    Port.Read(msgByteBuffer, 0, n); //считываем сообщение
                    //    string Message = Encoding.Default.GetString(msgByteBuffer);
                    //    Log.Invoke(new EventHandler(delegate
                    //    {
                    //        Log.AppendText("[" + DateTime.Now + "] : Получено подтверждение об успешной доставке Frame " + Message + "\n");
                    //    }));
                    //    SuccessfulFrameNumber = int.Parse(Message);

                    //    WriteData(null, FrameType.FILE);
                    //}
                    //else
                    //{
                    //    WriteData(null, FrameType.RET_MSG);
                    //}
                    //break;
                #endregion
                case FrameType.FILE:

                    #region FILE
                    if (IsConnected())
                    {
                        byte fileId = (byte)Port.ReadByte();
                        string typeFile = TypeFileAnalysis(fileId);
                       
                        byte[] size = new byte[sizeLenght];
                        Port.Read(size, 0, sizeLenght);
                        int ssize = (int)Double.Parse(Encoding.Default.GetString(size));
                        
                        byte[] byte_NumOfFrames = new byte[NumOfFrameLenght];
                        Port.Read(byte_NumOfFrames, 0, NumOfFrameLenght);
                        int NumOfFrames = (int)Double.Parse(Encoding.Default.GetString(byte_NumOfFrames));

                        byte[] byte_FrameNumber = new byte[NumOfFrameLenght];
                        Port.Read(byte_FrameNumber, 0, NumOfFrameLenght);
                        int FrameNumber = (int)Double.Parse(Encoding.Default.GetString(byte_FrameNumber));


                        if (FrameNumber == 1)
                        {
                            //DialogResult result;
                            
                            //result = MessageBox.Show("Файл. Размер: " + fileSize.ToString() + " Кбайт.\nПринять?", "Прием файла", MessageBoxButtons.YesNo);
                            //if (result == DialogResult.Yes)
                            //{
                                file_buffer = new byte[NumOfFrames*(Port.WriteBufferSize - 27)];
                            //}

                            //else
                            //{
                            //    Display.Invoke(new EventHandler(delegate
                            //    {
                            //        Display.AppendText(
                            //        "[" + DateTime.Now + "] : " + ": "
                            //        + "Вы не сохранили файл"
                            //        + "\r\n");
                            //        Display.ScrollToCaret();
                            //    }));
                            //}
                        }

                        Display.Invoke(new EventHandler(delegate
                        {
                            Display.AppendText(
                            "[" + DateTime.Now + "] : "
                            + "Сохраненный фрейм:"
                            + FrameNumber.ToString()
                            + "\r\n");
                            Display.ScrollToCaret();
                        }));
                        int n = Port.WriteBufferSize - InfoLen;
                        byte[] newPart = new byte[n];
                        Port.Read(newPart, 0, n);

                        newPart.CopyTo(file_buffer, n * (FrameNumber-1));
                        

                        if (FrameNumber==NumOfFrames)
                        {
                            Decoded = new byte[ssize];
                            ToDecode = new byte[2];

                            for (int i = 0; i < ssize; i++)
                            {
                                ToDecode[0] = file_buffer[i * 2];
                                ToDecode[1] = file_buffer[(i * 2) + 1];
                                Decoded[i] = Hamming.Decode(ToDecode);
                            }


                            SaveFileDialog saveFileDialog = new SaveFileDialog();

                            MainForm.Invoke(new EventHandler(delegate
                            {
                                saveFileDialog.FileName = "";
                                saveFileDialog.Filter = "TypeFile (*." + typeFile + ")|*." + typeFile + "|All files (*.*)|*.*";
                                if (DialogResult.OK == saveFileDialog.ShowDialog())
                                {
                                    File.WriteAllBytes(saveFileDialog.FileName, Decoded);
                                    //WriteData(null, FrameType.ACK);
                                    Display.Invoke(new EventHandler(delegate
                                    {
                                        Display.AppendText(
                                        "[" + DateTime.Now + "] : "
                                        + "Файл успешно получен"
                                        + "\r\n");
                                        Display.ScrollToCaret();
                                    }));
                                }
                                else
                                {
                                    // MessageBox.Show("Отмена ");
                                    Display.Invoke(new EventHandler(delegate
                                    {
                                        Display.AppendText(
                                        "[" + DateTime.Now + "] : " + ": "
                                        + "Вы не сохранили файл"
                                        + "\r\n");
                                        Display.ScrollToCaret();
                                    }));
                                }
                            }));
                        }

                        WriteData(FrameNumber.ToString(), FrameType.FRAME);
                            
                    }
                    else
                    {
                        WriteData(null, FrameType.ERR_FILE);
                    }

                    break;
                #endregion
                //======================================================



                case FrameType.ACK:
                    #region ACK
                    WriteData(FilePath, FrameType.FILE);
                    break;
                    #endregion

                case FrameType.RET_MSG:
                    #region RET_MSG
                    Log.AppendText("Message error! No connection\n");
                    break;
                #endregion

                case FrameType.ERR_FILE:
                    #region RET_FILE
                    Log.AppendText("File error! No connection\n");
                    break;
                    #endregion
            }
        }

        private ProgressBar _ProgressBar;
        public ProgressBar ProgressBar
        {
            get
            {
                return _ProgressBar;
            }
            set
            {
                _ProgressBar = value;
            }
        }

        private Button _b_SendFile;
        public Button b_SendFile
        {
            get
            {
                return _b_SendFile;
            }
            set
            {
                _b_SendFile = value;
            }
        }
        
        private RichTextBox _Log; //штука, чтобы видеть, что творится
        public RichTextBox Log
        {
            get
            {
                return _Log;
            }
            set
            {
                _Log = value;
            }
        }

        private Label _TestLabel;
        public Label TestLabel
        {
            get
            {
                return _TestLabel;
            }
            set
            {
                _TestLabel = value;
            }
        }

        private Form _mainForm;
        public Form MainForm
        {
            get
            {
                return _mainForm;
            }
            set
            {
                _mainForm = value;
            }
        }

        private RichTextBox _Display;
        /// <summary>
        /// Окно вывода сообщений
        /// </summary>
        public RichTextBox Display
        {
            get
            {
                return _Display;
            }
            set
            {
                _Display = value;
            }
        }
        private string TypeFileAnalysis(byte fileId)
        {
            switch (fileId)
            {
                case 1:
                    return "txt";
                case 2:
                    return "png";
                case 3:
                    return "pdf";
                case 4:
                    return "docx";
                case 5:
                    return "jpeg";
                case 6:
                    return "avi";
                case 7:
                    return "mp3";
                case 8:
                    return "rar";
                default:
                    return "typenotfound";
            }
        }

        private byte TypeFile_to_IdFile(string str)
        {
            switch (str)
            {
                case "txt":
                    return 1;
                case "png":
                    return 2;
                case "pdf":
                    return 3;
                case "docx":
                    return 4;
                case "jpeg":
                    return 5;
                case "avi":
                    return 6;
                case "mp3":
                    return 7;
                case "rar":
                    return 8;
                default:
                    return 9;
            }
        }

    }
}
