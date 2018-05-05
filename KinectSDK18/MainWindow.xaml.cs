using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect.Toolkit;
using System.Globalization;
using BingMapsRESTToolkit;
using Microsoft.Maps.MapControl.WPF;
using System.Net.Http;
using System.Threading;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Device.Location;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Xml;
using System.Diagnostics;
using System.Resources;
using Newtonsoft.Json.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;

using Microsoft.CognitiveServices.SpeechRecognition;
using System.Windows.Controls;
using Microsoft.Kinect.Toolkit.Controls;

namespace WPFKinectSDK18
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool flag = true;
        public static bool speechflag = true;

        private KinectSensor sensor;

        AutoResetEvent _FinalResponseEvent;
        MicrophoneRecognitionClient _microphoneRecognitionClient;

        double latitude = 0;
        double longitude = 0;
       // double t = 15;
        

        List<ItineraryItem> items = new List<ItineraryItem>();
        List<ResourceSet> resourceSet = new List<ResourceSet>();
        List<Microsoft.Maps.MapControl.WPF.Location> loc = new List<Microsoft.Maps.MapControl.WPF.Location>();
        Resource resource;
        public static string city,state = null;


        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindowLoaded;

            BingMap();
            getMyCurrentLocation();


            //map.Credentials = "Your_Bing_Maps_Key";
            //progress.Visibility = Visibility.Collapsed;
            // ConvertSpeechToText();
            //RecordButton.IsEnabled= true;
            //RecordButton.ClickMode();
            //  _FinalResponseEvent = new AutoResetEvent(false);

            // _FinalResponseEvent.Set();
            //RecordButton.Content = "Start1 Recording";
            _FinalResponseEvent = new AutoResetEvent(false);
            RecordButton_Click(new object(), new RoutedEventArgs());
            // RecordButton.Content = "Start Recording";
            // RecordButton_Click(new object(), new RoutedEventArgs());
            //OutputTextbox.Background = Brushes.White;
            //OutputTextbox.Foreground = Brushes.Black;


            
        }
        

       
        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            var sensorStatus = new KinectSensorChooser();

            sensorStatus.KinectChanged += KinectSensorChooserKinectChanged;
            kinectChooser.KinectSensorChooser = sensorStatus;
            sensorStatus.Start();

        }
        private void KinectSensorChooserKinectChanged(object sender, KinectChangedEventArgs e)
        {

            if (sensor != null)
                sensor.SkeletonFrameReady -= KinectSkeletonFrameReady;

            sensor = e.NewSensor;

            if (sensor == null)
                return;

            switch (Convert.ToString(e.NewSensor.Status))
            {
                case "Connected":
                    KinectStatus.Content = "Connected";
                    break;
                case "Disconnected":
                    KinectStatus.Content = "Disconnected";
                    break;
                case "Error":
                    KinectStatus.Content = "Error";
                    break;
                case "NotReady":
                    KinectStatus.Content = "Not Ready";
                    break;
                case "NotPowered":
                    KinectStatus.Content = "Not Powered";
                    break;
                case "Initializing":
                    KinectStatus.Content = "Initialising";
                    break;
                default:
                    KinectStatus.Content = "Undefined";
                    break;
            }

            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += KinectSkeletonFrameReady;

        }
        //private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        private void KinectSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeletons = new Skeleton[0];

            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            if (skeletons.Length == 0)
            {
                return;
            }

            var skel = skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);
            if (skel == null)
            {
                return;
            }

            var head = skel.Joints[JointType.Head];

            var rightHand = skel.Joints[JointType.WristRight];
            XValueRight.Text = rightHand.Position.X.ToString(CultureInfo.InvariantCulture);
            YValueRight.Text = rightHand.Position.Y.ToString(CultureInfo.InvariantCulture);
            ZValueRight.Text = rightHand.Position.Z.ToString(CultureInfo.InvariantCulture);

            var leftHand = skel.Joints[JointType.WristLeft];
            XValueLeft.Text = leftHand.Position.X.ToString(CultureInfo.InvariantCulture);
            YValueLeft.Text = leftHand.Position.Y.ToString(CultureInfo.InvariantCulture);
            ZValueLeft.Text = leftHand.Position.Z.ToString(CultureInfo.InvariantCulture);

            var centreHip = skel.Joints[JointType.HipCenter];

            SkeletonPoint pt = new SkeletonPoint();
            pt.X = ScaleVector(800, rightHand.Position.X);
            pt.Y = ScaleVector(600, -rightHand.Position.Y);
            pt.Z = rightHand.Position.Z;

            // t = myMap.TargetZoomLevel;

            if (rightHand.Position.X < leftHand.Position.X && centreHip.Position.Z - leftHand.Position.Z > 0.2 && centreHip.Position.Z - rightHand.Position.Z > 0.2)
            {

                RightRaised.Text = "Both Hands Crossed";
                LeftRaised.Text = "Both Hands Crossed";

                


                myMap.ZoomLevel = myMap.TargetZoomLevel + 0.1;
              //  t = t + 0.1;

            }
            else if (centreHip.Position.Z - leftHand.Position.Z > 0.2 && centreHip.Position.Z - rightHand.Position.Z < 0.2)
            {

                LeftRaised.Text = "Raised";

                var activeHand = rightHand.Position.Z <= leftHand.Position.Z ? rightHand : leftHand;

                var position = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                                                        activeHand.Position,
                                                        ColorImageFormat.RgbResolution640x480Fps30);

                cursor.Flip(activeHand);
                cursor.Update(position);


            }
            else if (centreHip.Position.Z - leftHand.Position.Z > 0.2 && centreHip.Position.Z - rightHand.Position.Z > 0.2 && rightHand.Position.X > leftHand.Position.X)
            {

                LeftRaised.Text = "Raised";
                RightRaised.Text = "Raised";
                if (myMap.TargetZoomLevel > 0)
                {
                    myMap.ZoomLevel = myMap.TargetZoomLevel - 0.1; //t;
                    //t = t - 0.1;
                }

            }


            else if (leftHand.Position.Y > head.Position.Y) {
                //canvas.SetValue(myMap, rightHand.Position.X);
                //Vector vector = new Vector();
                //vector.X = ScaleVector();
                LeftRaised.Text = "Head";
                
                rightHand.Position = pt;
                //DependencyObject ob = new DependencyObject();
                //MapLayer.SetPosition(ob , myMap.ZoomLevel);

                //cursor.MouseMove += Cursor_MouseMove;
                //myMap.MouseMove += Cursor_MouseMove;
                //DependencyObject dp;
                //Map.SetFlowDirection(dp, rightHand.Position.X);
                //Canvas.SetTop(myMap, rightHand.Position.Y);
                //scrollContent.Children.Add(myMap);

            }
            else
            {
                LeftRaised.Text = "Lowered";
                RightRaised.Text = "Lowered";
            }
        }

        

        private float ScaleVector(int length, float position)
        {
            float value = (((((float)length) / 1f) / 2f) * position) + (length / 2);
            if (value > length)
            {
                return (float)length;
            }
            if (value < 0f)
            {
                return 0f;
            }
            return value;
        }
        public void BingMap()
        {
            myMap.Focus();

            //myMap.Mode = new AerialMode(true);

            myMap.Center = new Microsoft.Maps.MapControl.WPF.Location();
        }


        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            RecordButton.Content = "Listening...\n(say break to stop Listening)";
            RecordButton.IsEnabled = false;
            OutputTextbox.Background = Brushes.Green;
            OutputTextbox.Foreground = Brushes.White;
            ConvertSpeechToText();
            speechflag = true;
        }
        //aae3e1e73cce4928a4effb2d862abd92
        //85b1fcaf09b34b25977757daa0c0b82d
        /// <summary>
        /// Start listening. 
        /// </summary>
        private void ConvertSpeechToText()
        {
            var speechRecognitionMode = SpeechRecognitionMode.ShortPhrase;
            string language = "en-us";
            string subscriptionKey = ConfigurationManager.AppSettings["SpeechKey"].ToString();
            //OutputTextbox.Text = subscriptionKey;
            _microphoneRecognitionClient
                    = SpeechRecognitionServiceFactory.CreateMicrophoneClient
                                    (
                                    speechRecognitionMode,
                                    language,
                                    subscriptionKey
                                    );

            _microphoneRecognitionClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            _microphoneRecognitionClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            _microphoneRecognitionClient.StartMicAndRecognition();

        }

        void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            string result = e.PartialResult;
            Dispatcher.Invoke(() =>
            {
                OutputTextbox.Text = (e.PartialResult);
                OutputTextbox.Text += ("\n");

            });
        }


        /// <summary>
        /// Speaker has finished speaking. Sever connection to server, stop listening, and clean up
        /// </summary>

        void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                _FinalResponseEvent.Set();
          //      _microphoneRecognitionClient.EndMicAndRecognition();
            //    _microphoneRecognitionClient.Dispose();
              //  _microphoneRecognitionClient = null;
                RecordButton.Content = "Start Recording";
                String result = OutputTextbox.Text;


                PrintCityState(GetCityState(result));


                RecordButton.IsEnabled = true;
                OutputTextbox.Background = Brushes.White;
                OutputTextbox.Foreground = Brushes.Black;
                ParseString(result);
                OutputTextbox.Text = null;
                

            }));
            //ConvertSpeechToText();
        }
        void ParseString(String result)
        {
            //if (String.IsNullOrEmpty(result)) return;
            char[] delimiterChars = { ' ', ',', '.', ':', '\t', '\n' };
            string[] words = result.Split(delimiterChars);
            String resultString = null;
            int zoomLevel = 3;
            int getZoom = 0;
            List<string> latitudeArr = new List<string>();
            List<string> longitudeArr = new List<string>();
            List<string> NameArr = new List<string>();
            XmlDocument xmldoc = new XmlDocument();
            XmlNodeList nodeList;

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] == "break")
                {
                    OutputTextbox.Text = null;
                    speechflag = false;
                    //RecordButton_Click(new object(), new RoutedEventArgs());
                    ////  _FinalResponseEvent = new AutoResetEvent(false);
                    //RecordButton.Content = "What Do You Want?";
                    //OutputTextbox.Background = Brushes.Blue;
                    //OutputTextbox.Foreground = Brushes.Black;
                }
                if (words[i] == "zoom")
                {
                    if (words[i + 1] == "in")
                    {
                        if (result.Any(c => char.IsDigit(c)))
                        {
                            resultString = Regex.Match(result, @"\d+").Value;
                            getZoom = Int32.Parse(resultString);
                        }
                        if (getZoom > 0)
                            myMap.ZoomLevel = getZoom + myMap.TargetZoomLevel;
                        else
                            myMap.ZoomLevel = zoomLevel + myMap.TargetZoomLevel;
                    }


                    else if (words[i + 1] == "out")
                    {
                        if (result.Any(c => char.IsDigit(c)))
                        {
                            resultString = Regex.Match(result, @"\d+").Value;
                            getZoom = Int32.Parse(resultString);
                        }
                        if (getZoom > 0)
                            myMap.ZoomLevel = myMap.TargetZoomLevel - getZoom;
                        else
                            myMap.ZoomLevel = myMap.TargetZoomLevel - zoomLevel;
                    }
                }


                if (words[i] == "restaurants" || words[i] == "restaurant")
                {
                    latitudeArr.Clear();
                    longitudeArr.Clear();
                    NameArr.Clear();

                    xmldoc.LoadXml(GET(5800, latitude, longitude));
                    nodeList = xmldoc.GetElementsByTagName("d:Latitude");
                    foreach (XmlNode node in nodeList)
                    {
                        latitudeArr.Add(node.InnerText);
                    }

                    nodeList = xmldoc.GetElementsByTagName("d:Longitude");
                    foreach (XmlNode node in nodeList)
                    {
                        longitudeArr.Add(node.InnerText);

                    }
                    nodeList = xmldoc.GetElementsByTagName("d:DisplayName");
                    foreach (XmlNode node in nodeList)
                    {
                        NameArr.Add(node.InnerText);
                    }

                    for (int a = 0; a < latitudeArr.ToArray().Length; a++)
                    {
                        //TestTextbox.Text += latitudeArr[a] + "," + longitudeArr[a] + "\n";
                        //pushpin.AddPushPin(46.8442643, 2.5992004);

                        Pushpin pushpin1 = new Pushpin();
                        pushpin1.ToolTip = NameArr[a];
                        pushpin1.Background = new SolidColorBrush(Color.FromArgb(200, 120, 120, 120));
                        MapLayer.SetPosition(pushpin1, new Microsoft.Maps.MapControl.WPF.Location((Convert.ToDouble(latitudeArr[a])), Convert.ToDouble(longitudeArr[a])));
                        myMap.Children.Add(pushpin1);

                    }
                }
                if (words[i] == "cinema" || words[i] == "cinemas")
                {
                    latitudeArr.Clear();
                    longitudeArr.Clear();
                    NameArr.Clear();

                    xmldoc.LoadXml(GET(7832, latitude, longitude));
                    nodeList = xmldoc.GetElementsByTagName("d:Latitude");
                    foreach (XmlNode node in nodeList)
                    {
                        
                        latitudeArr.Add(node.InnerText);
                    }

                    nodeList = xmldoc.GetElementsByTagName("d:Longitude");
                    foreach (XmlNode node in nodeList)
                    {
                        longitudeArr.Add(node.InnerText);

                    }
                    nodeList = xmldoc.GetElementsByTagName("d:DisplayName");
                    foreach (XmlNode node in nodeList)
                    {
                        NameArr.Add(node.InnerText);
                    }

                    for (int b = 0; b < latitudeArr.ToArray().Length; b++)
                    {
                        //TestTextbox.Text += latitudeArr[a] + "," + longitudeArr[a] + "\n";
                        //pushpin.AddPushPin(46.8442643, 2.5992004);

                        Pushpin pushpin2 = new Pushpin();
                        pushpin2.ToolTip = NameArr[b];
                        pushpin2.Background = new SolidColorBrush(Color.FromArgb(200, 66, 134, 244));
                        MapLayer.SetPosition(pushpin2, new Microsoft.Maps.MapControl.WPF.Location((Convert.ToDouble(latitudeArr[b])), Convert.ToDouble(longitudeArr[b])));
                        myMap.Children.Add(pushpin2);

                    }
                }


                if (words[i] == "route")
                {
                    if (words[i + 1] == "to")
                    {
                        if (city != null)
                        {
                            try
                            {
                                var rootObject = JsonConvert.DeserializeObject<RootObject>(getCoordinates());
                                foreach (ResourceSet set in rootObject.resourceSets)
                                {
                                    resourceSet.Add(set);
                                }

                                loc.Clear();
                                resource = resourceSet[0].resources[0];
                                items = resource.routeLegs[0].itineraryItems;

                                // Colleting location points to draw route got in response. 
                                foreach (ItineraryItem item in items)

                                {
                                    loc.Add(new Microsoft.Maps.MapControl.WPF.Location() { Latitude = item.maneuverPoint.coordinates[0], Longitude = item.maneuverPoint.coordinates[1] });
                                }

                                loc.ToArray();

                                //TestTextbox.Text = loc.ToArray().Length.ToString();
                                for (int b = 0; b < loc.ToArray().Length - 1; b++)
                                {
                                    MapPolyline polyline = new MapPolyline();
                                    polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Purple);
                                    polyline.StrokeThickness = 5;
                                    polyline.Opacity = 1;
                                    polyline.Locations = new LocationCollection() {
                     new Microsoft.Maps.MapControl.WPF.Location(loc[b]),
                     new Microsoft.Maps.MapControl.WPF.Location(loc[b+1]),
                    };


                                    myMap.Children.Add(polyline);

                                }
                            }
                            catch (WebException ex)
                            {
                                MessageBox.Show("No Path Found By Road For Your Destination!! ");

                                ex.GetBaseException();
                            }
                            }
                    }
                    }
                
                resourceSet.Clear();
                
            }
            if(speechflag)
            RecordButton_Click(new object(), new RoutedEventArgs());

        }

        void getMyCurrentLocation()
        {


            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoPositionChanged);

            watcher.Start();




        }



        private void GeoPositionChanged(object sender,
    GeoPositionChangedEventArgs<GeoCoordinate> e)
        {



            if (flag)
            {
                Pushpin pushpin = new Pushpin();
                pushpin.ToolTip = "Current Location";
                flag = false;
                myMap.ZoomLevel = 15;
                MapLayer.SetPosition(pushpin, new Microsoft.Maps.MapControl.WPF.Location(e.Position.Location.Latitude, e.Position.Location.Longitude));
                myMap.Children.Add(pushpin);

            }


            myMap.Center = new Microsoft.Maps.MapControl.WPF.Location(e.Position.Location.Latitude, e.Position.Location.Longitude);
            latitude = e.Position.Location.Latitude;
            longitude = e.Position.Location.Longitude;
        }


        string GET(int poi, double lat, double lon)
        {
            string url = "http://spatial.virtualearth.net/REST/v1/data/c2ae584bbccc4916a0acf75d1e6947b4/NavteqEU/NavteqPOIs?spatialFilter=nearby(" + lat + "," + lon + ",5)&$filter=EntityTypeID%20eq%20%27" + poi + "%27&key=ArL4GQSvtpakxJhd_Ym5IbVpxNTkUDBSZ2nfJlYEHLLNXwggEbjKD2HQpiKU-kJR";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();

                }
                throw;
            }
        }


        public string getCoordinates()
        
        {
           

                //51.509865,-0.118092
                string url = "http://dev.virtualearth.net/REST/V1/Routes?wp.0=" + latitude + "," + longitude + "&wp.1=" + city + "&key=ArL4GQSvtpakxJhd_Ym5IbVpxNTkUDBSZ2nfJlYEHLLNXwggEbjKD2HQpiKU-kJR";
                //string url = "http://dev.virtualearth.net/REST/V1/Routes/Driving?wp.0=Minneapolis,MN&wp.1=St%20Paul,MN&optmz=distance&routeAttributes=routePath&key=ArL4GQSvtpakxJhd_Ym5IbVpxNTkUDBSZ2nfJlYEHLLNXwggEbjKD2HQpiKU-kJR";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                try
                {
                    WebResponse response = request.GetResponse();
               
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        return reader.ReadToEnd();
                    }
                }
                catch (WebException ex)
                {
                    WebResponse errorResponse = ex.Response;
                    using (Stream responseStream = errorResponse.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                        String errorText = reader.ReadToEnd();

                    }
                    throw;
                }
            
        }
        private void drawRoute_Click(object sender, RoutedEventArgs e)
        {
            ////getCoordinates();
            ////var obj = JObject.Parse(getCoordinates());
            ////var url = (string)obj.SelectToken("maneuverPoint.coordinates");
            //// Parsing JSON Response
            //if (city != null)
            //{
            //    var rootObject = JsonConvert.DeserializeObject<RootObject>(getCoordinates());
            //    foreach (ResourceSet set in rootObject.resourceSets)
            //    {
            //        resourceSet.Add(set);
            //    }

            //    loc.Clear();
            //    resource = resourceSet[0].resources[0];
            //    items = resource.routeLegs[0].itineraryItems;

            //    // Colleting location points to draw route got in response. 
            //    foreach (ItineraryItem item in items)

            //    {
            //        loc.Add(new Microsoft.Maps.MapControl.WPF.Location() { Latitude = item.maneuverPoint.coordinates[0], Longitude = item.maneuverPoint.coordinates[1] });
            //    }

            //    loc.ToArray();

            //    // TestTextbox.Text = loc.ToArray().Length.ToString();
            //    for (int b = 0; b < loc.ToArray().Length - 1; b++)
            //    {
            //        MapPolyline polyline = new MapPolyline();
            //        polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Purple);
            //        polyline.StrokeThickness = 5;
            //        polyline.Opacity = 1;
            //        polyline.Locations = new LocationCollection() {
            //         new Microsoft.Maps.MapControl.WPF.Location(loc[b]),
            //         new Microsoft.Maps.MapControl.WPF.Location(loc[b+1]),
            //        };


            //        myMap.Children.Add(polyline);

            //    }
            //}
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            
            //    recEngine.RecognizeAsync(RecognizeMode.Single);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            //Application.Exit();
        }
        //private void Onload(object sender, EventArgs e)
        //{
        //    TestTextbox.Text = "home";
        //    Choices commands = new Choices();
        //    commands.Add(new string[] { "hello", "hey", "jarvis" });
        //    GrammarBuilder gBuilder = new GrammarBuilder();
        //    gBuilder.Append(commands);
        //    Grammar grammar = new Grammar(gBuilder);

        //    recEngine.LoadGrammarAsync(grammar);
        //    recEngine.SetInputToDefaultAudioDevice();
        //    recEngine.SpeechRecognized += recEngine_SpeechRecognized;

            

        //    void recEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {

        //        switch (e.Result.Text)
        //        {
        //            case "hello":
        //                MessageBox.Show("hi handsome how are you");
        //                break;
        //            case "hey":
        //                TestTextbox.Text = ("hey what you doing");
        //                break;

        //        }

        //    }
        public  void PrintCityState(CityState cs)
        {
            if(cs.City!=null)
            {
                city = cs.City;
                state = cs.StateName;
                //TestTextbox.Text = city + " + " + state; /*cs.City +" "+ cs.StateAbbreviation + " " + cs.StateName;  */
            }

            // Console.WriteLine("{0}, {1} ({2})", cs.City, cs.StateAbbreviation, cs.StateName);
        }

       

        public static CityState GetCityState(string input)
        {
            string truncatedInput = input;
            var statesDictionary = new Dictionary<string, string>
        {

            {"GE", "Germany"},
            {"EN", "England"},
            {"SP", "Spain"},
            {"FR", "France"},
            {"IT", "Italy"},
            {"PK", "Pakistan"},
            {"IN", "India"},
            {"RU", "Russia"}
            // And so forth for all 50 states
        };
            var cityState = new CityState();

            foreach (KeyValuePair<string, string> kvp in statesDictionary)
            {
                if (input.Trim().ToLower().EndsWith(" " + kvp.Key.ToLower()))
                {
                    cityState.StateName = kvp.Value;
                    cityState.StateAbbreviation = kvp.Key;
                    truncatedInput = input.Remove(input.Length - 1 - kvp.Key.Length);
                    break;
                }
                if (input.Trim().ToLower().EndsWith(" " + kvp.Value.ToLower()))
                {
                    cityState.StateName = kvp.Value;
                    cityState.StateAbbreviation = kvp.Key;
                    truncatedInput = input.Remove(input.Length - 1 - kvp.Value.Length);
                    break;
                }
            }

            cityState.City = truncatedInput.Trim().Trim(',').Trim();
            return cityState;
        }
        
    }




    class Program
    {
        //static void Main(string[] args)
        //{
        //    PrintCityState(GetCityState("Grand Rapids, New Mexico"));
        //    PrintCityState(GetCityState("Sacremento California"));
        //    PrintCityState(GetCityState("Indianpolis, IN"));
        //    PrintCityState(GetCityState("Phoenix AZ"));
        //}

        
    }

    public class CityState
    {
        public string City { get; set; }
        public string StateName { get; set; }
        public string StateAbbreviation { get; set; }
    }
    





    public class ActualEnd
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    public class ActualStart
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    public class Detail
    {
        public int compassDegrees { get; set; }
        public List<int> endPathIndices { get; set; }
        public List<string> locationCodes { get; set; }
        public string maneuverType { get; set; }
        public string mode { get; set; }
        public List<string> names { get; set; }
        public string roadType { get; set; }
        public List<int> startPathIndices { get; set; }
    }

    public class Instruction
    {
        public object formattedText { get; set; }
        public string maneuverType { get; set; }
        public string text { get; set; }
    }

    public class ManeuverPoint
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
    }

    public class Warning
    {
        public string origin { get; set; }
        public string severity { get; set; }
        public string text { get; set; }
        public string to { get; set; }
        public string warningType { get; set; }
    }

    public class Hint
    {
        public string hintType { get; set; }
        public string text { get; set; }
    }

    public class ItineraryItem
    {
        public string compassDirection { get; set; }
        public List<Detail> details { get; set; }
        public string exit { get; set; }
        public string iconType { get; set; }
        public Instruction instruction { get; set; }
        public ManeuverPoint maneuverPoint { get; set; }
        public string sideOfStreet { get; set; }
        public string tollZone { get; set; }
        public string towardsRoadName { get; set; }
        public string transitTerminus { get; set; }
        public double travelDistance { get; set; }
        public int travelDuration { get; set; }
        public string travelMode { get; set; }
        public List<string> signs { get; set; }
        public List<Warning> warnings { get; set; }
        public List<Hint> hints { get; set; }
    }

    public class EndWaypoint
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
        public string description { get; set; }
        public bool isVia { get; set; }
        public string locationIdentifier { get; set; }
        public int routePathIndex { get; set; }
    }

    public class StartWaypoint
    {
        public string type { get; set; }
        public List<double> coordinates { get; set; }
        public string description { get; set; }
        public bool isVia { get; set; }
        public string locationIdentifier { get; set; }
        public int routePathIndex { get; set; }
    }

    public class RouteSubLeg
    {
        public EndWaypoint endWaypoint { get; set; }
        public StartWaypoint startWaypoint { get; set; }
        public double travelDistance { get; set; }
        public int travelDuration { get; set; }
    }

    public class RouteLeg
    {
        public ActualEnd actualEnd { get; set; }
        public ActualStart actualStart { get; set; }
        public List<object> alternateVias { get; set; }
        public int cost { get; set; }
        public string description { get; set; }
        public List<ItineraryItem> itineraryItems { get; set; }
        public string routeRegion { get; set; }
        public List<RouteSubLeg> routeSubLegs { get; set; }
        public double travelDistance { get; set; }
        public int travelDuration { get; set; }
    }

    public class Resource
    {
        public string __type { get; set; }
        public List<double> bbox { get; set; }
        public string id { get; set; }
        public string distanceUnit { get; set; }
        public string durationUnit { get; set; }
        public List<RouteLeg> routeLegs { get; set; }
        public string trafficCongestion { get; set; }
        public string trafficDataUsed { get; set; }
        public double travelDistance { get; set; }
        public int travelDuration { get; set; }
        public int travelDurationTraffic { get; set; }
    }

    public class ResourceSet
    {
        public int estimatedTotal { get; set; }
        public List<Resource> resources { get; set; }
    }

    public class RootObject
    {
        public string authenticationResultCode { get; set; }
        public string brandLogoUri { get; set; }
        public string copyright { get; set; }
        public List<ResourceSet> resourceSets { get; set; }
        public int statusCode { get; set; }
        public string statusDescription { get; set; }
        public string traceId { get; set; }
    }
}
