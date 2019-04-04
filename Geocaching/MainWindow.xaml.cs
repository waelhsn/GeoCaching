using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Geocaching
{
    public class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geochache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<FoundGeocache>().HasKey(fg => new { fg.PersonID, fg.GeocacheID });
            model.Entity<Geocache>()
                .HasOne(k => k.Person)
                .WithMany(p => p.Geocaches);

        }
    }
    public class Person
    {
        //[Key]
        public int ID { get; set; }

        [Column(TypeName = "nvarchar(50)"), Required]
        public string FirstName { get; set; }

        [Column(TypeName = "nvarchar(50)"), Required]
        public string LastName { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Column(TypeName = "nvarchar(50)"), Required]
        public string Country { get; set; }

        [Column(TypeName = "nvarchar(50)"), Required]
        public string City { get; set; }

        [Column(TypeName = "nvarchar(50)"), Required]
        public string StreetName { get; set; }
        public byte StreetNumber { get; set; }

        public List<Geocache> Geocaches { get; set; }

    }
    public class Geocache
    {
        public int ID { get; set; }

        public int? PersonID { get; set; }
       
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Column(TypeName = "nvarchar(255)"), Required]
        public string Contents { get; set; }

        [Column(TypeName = "nvarchar(255)"), Required]
        public string Message { get; set; }

        public Person Person { get; set; }
    }
    public class FoundGeocache
    {
        [ForeignKey("PersonID")]
        public int PersonID { get; set; }
        public Person Person { get; set; }


        [ForeignKey("GeocacheID")]
        public int GeocacheID { get; set; }
        public Geocache Geocache { get; set; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key
        private const string applicationId = "1KlpjniiHn1oXPu7OmLd~-RJENhz40F2_RPqM7GJdWA~Av_cdSualyUi8UW8s6omXTT2_USITW0gcrUwkcWj-hQ50_MbsPK4BhJ1D0l1-JL2";

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private Location latestClickLocation;

        private Location gothenburg = new Location(57.719021, 11.991202);

        static AppDbContext database;

        public int selectedPerson { get; set; }
        public int selectedGeo { get; set; }
        public bool IsSelected { get; set;}
        public List<Person> personList { get; set; }


        public MainWindow()
        {
            // Load data from database and populate map here.
            database = new AppDbContext();

            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            CreateMap();
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation = map.ViewportPointToLocation(point);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    
                    OnMapLeftClick();
                }
            };

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
            

            foreach (var item in database.Person)
            {
                Location personLocation = new Location(item.Latitude, item.Longitude);

                var pin = AddPin(personLocation, item.FirstName+ " " +item.LastName + "\n"+ item.StreetName+" "+ item.StreetNumber + "\n" + item.City ,  Colors.Blue);

                pin.MouseDown += (s, a) =>
                {   
                  //  perosnsList.Add(item);
                    // Handle click on geocache pin here.
                    pin.Background = new SolidColorBrush(Colors.Green);
                    selectedPerson = item.ID;
                    MessageBox.Show("You have selected:" + "\n" + item.FirstName + " "+ item.LastName + "\n" + item.StreetName +" "+item.StreetNumber);
                    UpdateMap();
                    a.Handled = true;
                    //SaveAllPersons();
                    //AddPin(personLocation, item.FirstName,Colors.Blue);
                };
               
            }
            foreach (var item in database.Geochache)
            {
                Location geocacheLocation = new Location(item.Latitude, item.Longitude);
                var pin = AddPin(geocacheLocation, item.Message, Colors.Gray);

                pin.MouseDown += (s, a) =>
                {
                    selectedGeo = item.ID;
                    pin.Opacity = .5;
                    // Handle click on geocache pin here.
                    if (item.PersonID != null)
                    {
                        try
                        {
                            FoundGeocache found = new FoundGeocache
                            {
                                GeocacheID = selectedGeo,
                                PersonID = selectedPerson,
                            };
                            database.FoundGeocache.Add(found);
                            database.SaveChanges();
                        }
                        catch
                        {
                            MessageBox.Show("You have already claimed it.");
                        }
                        MessageBox.Show(item.Contents + "\n" + item.Message + "\nPlaced by: " + item.Person.FirstName);
                    }
                    else
                    {
                        MessageBox.Show(item.Contents + "\n" + item.Message + "\nPlaced by: UNKNOWN");
                    }

                    UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }
        }

        private void UpdateMap()
        {
            
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            var dialog = new GeocacheDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            try

            {

            // Add geocache to map and database here.
            Geocache g = new Geocache
            {
                PersonID = selectedPerson,
                Contents = dialog.GeocacheContents,
                Message = dialog.GeocacheMessage,
                Latitude = latestClickLocation.Latitude,
                Longitude = latestClickLocation.Longitude,
            };
            database.Add(g);
            database.SaveChanges();
            }
            catch
            {
                  MessageBox.Show("Please Select a person from the map, and then add a new GeoCache to that person you have selected.");
            }

            foreach (var item in database.Geochache)
            {
            var pin = AddPin(latestClickLocation, "Person", Colors.Gray);


            pin.MouseDown += (s, a) =>
            {
                // Handle click on geocache pin here.
                MessageBox.Show(item.Person.FirstName);
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
            }
            
        }
        private void SaveAllPersons()
        {

            StreamWriter SavePersons = new StreamWriter("Geocaches.txt");

            foreach (var item in database.FoundGeocache)
            {
                //const string persons = "Geocaches.txt";
                string combinedString = item.Person.FirstName +
                    " | " + item.Person.LastName +
                    " | " + item.Person.Country +
                    " | " + item.Person.City +
                    " | " + item.Person.StreetName +
                    " "   + item.Person.StreetNumber +
                    " | " + item.Person.Latitude +
                    " | " + item.Person.Longitude +
                    "\n" + item.Geocache.PersonID +
                    " | " + item.Geocache.Latitude +
                    " | " + item.Geocache.Longitude +
                    " | " + item.Geocache.Contents+
                    " | " + item.Geocache.Message+
                    "\n"+ "Found: " + item.PersonID +
                    ", " + item.GeocacheID+ "\n";
                SavePersons.WriteLine(combinedString);

            }
            SavePersons.Close();
            //foreach (var item2 in database.Geochache)
            //{
            //    string combindString = item2.ID + "|" + item2.Latitude + "|" + item2.Longitude + "|" + item2.Contents + "|" + item2.Message;
            //    SavePersons.WriteLine(combindString);
            //}
            //foreach (var item3 in database.FoundGeocache)
            //{
            //    string combindString = "Found: " + item3.PersonID + "," + item3.GeocacheID;
            //    SavePersons.WriteLine(combindString);
            //}
            
        }


        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            };

            //using (database = new AppDbContext())
            //{
                Person p = new Person
                {
                    FirstName = dialog.PersonFirstName,
                    LastName = dialog.PersonLastName,
                    Latitude = latestClickLocation.Latitude,
                    Longitude = latestClickLocation.Longitude,
                    City = dialog.AddressCity,
                    Country = dialog.AddressCountry,
                    StreetName = dialog.AddressStreetName,
                    StreetNumber = dialog.AddressStreetNumber,
                };

                // Add person to map and database here.
                database.Add(p);
                database.SaveChanges();
            //}

            var pin = AddPin(latestClickLocation, "Person", Colors.Blue);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
                pin.Background = new SolidColorBrush(Colors.Green);
                selectedPerson = p.ID;
                MessageBox.Show("Clicked");
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private Pushpin AddPin(Location location, string tooltip, Color color)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Read the selected file here.
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            SaveAllPersons();
            //var dialog = new Microsoft.Win32.SaveFileDialog();
            //dialog.DefaultExt = ".txt";
            //dialog.Filter = "Text documents (.txt)|*.txt";
            //dialog.FileName = "Geocaches";
            //bool? result = dialog.ShowDialog();
            //if (result != true)
            //{
            //    return;
            //}
            //string path = dialog.FileName;
            // Write to the selected file here.
        }
    }
}
