using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Geocaching
{
    public class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Person)
                .WithMany(p => p.FoundGeocaches)
                .HasForeignKey(fg => fg.PersonId);

            model.Entity<FoundGeocache>()
                .HasOne(fg => fg.Geocache)
                .WithMany(g => g.FoundGeocaches)
                .HasForeignKey(fg => fg.GeocacheId);
        }
    }

    public class Person
    {
        public int ID { get; set; }
        [Column(TypeName = "nvarchar(50)"), Required]
        public string FirstName { get; set; }
        [Column(TypeName = "nvarchar(50)"), Required]
        public string LastName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [Column(TypeName = "nvarchar(50)"),Required]
        public string Country { get; set; }
        [Column(TypeName = "nvarchar(50)"),Required]
        public string City { get; set; }
        [Column(TypeName = "nvarchar(50)"),Required]
        public string StreetName { get; set; }
        public byte StreetNumber { get; set; }
        public ICollection<FoundGeocache> FoundGeocaches { get; set; }
    }

    public class Geocache
    {
        public int ID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [Column(TypeName = "nvarchar(255)"),Required]
        public string Contents { get; set; }
        [Column(TypeName = "nvarchar(255)"),Required]
        public string Message { get; set; }
        public int? PersonId { get; set; }
        public Person Person { get; set; }
        public ICollection<FoundGeocache> FoundGeocaches { get; set; }
    }

    public class FoundGeocache
    {
        public int ID { get; set; }

        [ForeignKey("PersonId")]
        public int PersonId { get; set; }
        public Person Person { get; set; }

        [ForeignKey("GeocacheId")]
        public int GeocacheId { get; set; }
        public Geocache Geocache { get; set; }
    }

    public partial class MainWindow : Window
    {
        private const string applicationId = "1KlpjniiHn1oXPu7OmLd~-RJENhz40F2_RPqM7GJdWA~Av_cdSualyUi8UW8s6omXTT2_USITW0gcrUwkcWj-hQ50_MbsPK4BhJ1D0l1-JL2";
        private MapLayer layer;
        private GeoCoordinate latestClickLocation;
        private GeoCoordinate gothenburg = new GeoCoordinate { Latitude = 57.719021, Longitude = 11.991202 };
        private GeoCoordinate geoCoo;
        private Person selectedPerson = null;
        private AppDbContext database = new AppDbContext();
        Object lockThis = new Object();
        public MainWindow()
        {
            latestClickLocation = gothenburg;
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            CreateMap();
        }

        private async void CreateMap()
        {
            try
            {
                layer.Children.Clear();
            }
            catch {}
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = new Location { Latitude = gothenburg.Latitude, Longitude = gothenburg.Longitude };
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation.Latitude = map.ViewportPointToLocation(point).Latitude;
                latestClickLocation.Longitude = map.ViewportPointToLocation(point).Longitude;

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    selectedPerson = null;
                    foreach (Pushpin pin in layer.Children)
                    {
                        try
                        {
                            Geocache geocache = (Geocache)pin.Tag;
                            UpdatePin(pin, Colors.Gray, 1);
                        }
                        catch
                        {
                            Person person = (Person)pin.Tag;
                            UpdatePin(pin, Colors.Blue, 1);
                        }
                    }
                }
            };

            var gcs = await Task.Run(() =>
            {
                return database.Geocache.Include(g => g.Person);
            });

            foreach (Geocache g in gcs)
            {
                geoCoo = new GeoCoordinate();
                geoCoo.Latitude = g.Latitude;
                geoCoo.Longitude = g.Longitude;
                var pin = AddPin(geoCoo, g.Message, Colors.Gray, 1, g);
            }

            var ppl = await Task.Run(() =>
            {
                return database.Person.ToArray();
            });

            foreach (Person person in ppl)
            {
                geoCoo = new GeoCoordinate();
                geoCoo.Latitude = person.Latitude;
                geoCoo.Longitude = person.Longitude;
                var pin = AddPin(geoCoo, person.FirstName +
                    " " + person.LastName + 
                    "\n" + person.StreetName +
                    " " + person.StreetNumber + 
                    "\n" + person.City ,  Colors.Blue, 1, person);

                pin.MouseDown += PersonSel;
            }

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private void PersonSel(object sender, MouseButtonEventArgs e)
        {
            Geocache[] geocaches = null;
            var gcs = Task.Run(() =>
            { geocaches = database.Geocache.Select(a => a).ToArray(); });

            Pushpin pin = (Pushpin)sender;
            Person person = (Person)pin.Tag;
            string tooptipp = pin.ToolTip.ToString();
            selectedPerson = person;
            UpdatePin(pin, Colors.Blue, 1);

            Task.WaitAll(gcs);

            foreach (Pushpin p in layer.Children)
            {

                try { p.MouseDown -= GreenPin; }
                catch {}
                try { p.MouseDown -= RedPin; }
                catch {}
                try { p.MouseDown -= Handled; }
                catch {}

                Geocache geocache = geocaches
                    .FirstOrDefault(g => g.Longitude == p.Location.Longitude && g.Latitude == p.Location.Latitude);

                FoundGeocache foundGeocache = null;
                if (geocache != null)
                {
                    foundGeocache = database.FoundGeocache
                        .FirstOrDefault(fg => fg.GeocacheId == geocache.ID && fg.PersonId == person.ID);
                }

                if (geocache == null && p.ToolTip.ToString() != tooptipp)
                {
                    UpdatePin(p, Colors.Blue, 0.5);
                }

                else if (geocache != null && geocache.PersonId == person.ID)
                {
                    UpdatePin(p, Colors.Black, 1);
                    p.MouseDown += Handled;
                }

                else if (geocache != null && foundGeocache != null)
                {
                    UpdatePin(p, Colors.Green, 1);
                    p.MouseDown += GreenPin;
                }

                else if (geocache != null && foundGeocache == null)
                {
                    UpdatePin(p, Colors.Red, 1);
                    p.MouseDown += RedPin;
                }
                e.Handled = true;
            }
        }

        private void GreenPin(object sender, MouseButtonEventArgs e)
        {
            
            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;
            try
            {
                FoundGeocache foundGeocache = database.FoundGeocache
               .FirstOrDefault(fg => fg.PersonId == selectedPerson.ID && fg.GeocacheId == geocache.ID);
            database.Remove(foundGeocache);
            }
            catch { }

            try { database.SaveChanges(); }
            catch { }
            UpdatePin(pin, Colors.Red, 1);
            pin.MouseDown -= GreenPin;
            pin.MouseDown += RedPin;
            e.Handled = true;
        }

        private void RedPin(object sender, MouseButtonEventArgs e)
        {
            Pushpin pin = (Pushpin)sender;
            Geocache geocache = (Geocache)pin.Tag;
            FoundGeocache foundGeocache = new FoundGeocache
            {
                Person = selectedPerson,
                Geocache = geocache
            };
            database.Add(foundGeocache);
            try { database.SaveChanges(); }
            catch { }
            UpdatePin(pin, Colors.Green, 1);
            pin.MouseDown -= RedPin;
            pin.MouseDown += GreenPin;
            e.Handled = true;
        }

        private void Handled(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UpdatePin(Pushpin pin, Color color, double opacity)
        {
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
        }

        private async void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            if (selectedPerson != null)
            {
                var dialog = new GeocacheDialog();
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == false)
                {
                    return;
                }

                Geocache g = new Geocache
                {
                    PersonId = selectedPerson.ID,
                    Contents = dialog.GeocacheContents,
                    Message = dialog.GeocacheMessage,
                    Latitude = latestClickLocation.Latitude,
                    Longitude = latestClickLocation.Longitude,
                };
                await database.AddAsync(g);
                await database.SaveChangesAsync();

                GeoCoordinate geo = new GeoCoordinate();
                geo.Latitude = g.Latitude;
                geo.Longitude = g.Longitude;

                var pin = AddPin(geo, g.Message, Colors.Black, 1, g);
                pin.MouseDown += Handled;
            }
            else
            {
                MessageBox.Show("Please Select a person from the map, and then add a new GeoCache to that person you have selected.");
            }
        }

        private async void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == false)
            {
                return;
            };


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
            await database.AddAsync(p);
            await database.SaveChangesAsync();

            GeoCoordinate geo = new GeoCoordinate();
            geo.Latitude = p.Latitude;
            geo.Longitude = p.Longitude;

            var pin = AddPin(geo, p.FirstName + " " + p.LastName, Colors.Blue, 1, p);

            selectedPerson = p;

            pin.MouseDown += PersonSel;
        }

        private Pushpin AddPin(GeoCoordinate loc, string tooltip, Color color, double opacity, object obj)
        {
            var location = new Location { Latitude = loc.Latitude, Longitude = loc.Longitude };
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Opacity = opacity;
            pin.Location = location;
            pin.Tag = obj;
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, location);
            return pin;
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            List<string> list = new List<string>();
            string path = dialog.FileName;

            Task readFile = Task.Run(async() =>
            {
                await Task.WhenAll();
                Person[] ppl = database.Person
                .Select(p => p)
                .OrderByDescending(o => o)
                .ToArray();
                lock(lockThis)
                {
                    foreach (Person p in ppl)
                    {
                        list.Add( p.FirstName +
                            " | " + p.LastName + 
                            " | " + p.Country +
                            " | " + p.City +
                            " | " + p.StreetName +
                            " | " + p.StreetNumber +
                            " | " +p.Latitude +
                            " | "+p.Longitude); 

                        Geocache[] geo = database.Geocache
                            .Where(g => g.PersonId == p.ID)
                            .OrderByDescending(o => o).ToArray();

                        geo.ToList().ForEach(g => list.Add(g.ID +
                            " | " + g.Latitude+
                            " | " + g.Longitude+
                            " | " +g.Contents +
                            " | " + g.Message));

                        FoundGeocache[] founds = database.FoundGeocache
                            .Where(f => f.PersonId == p.ID)
                            .OrderByDescending(o => o).ToArray();

                        string allGeoID = "";
                        for (int i = 0; i < founds.Length; i++)
                        {
                            allGeoID += founds[i].GeocacheId;
                            if (i < founds.Length - 1)
                            {
                                allGeoID += ", ";
                            }
                        }

                        list.Add("Found: " + allGeoID);
                        list.Add("");
                    }
                }
            });
            Task.WaitAll(readFile);
            File.WriteAllLines(path, list);
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            Task RemoveDb = Task.Run(() =>
            {
                database.Person.RemoveRange(database.Person);
                database.Geocache.RemoveRange(database.Geocache);
                database.FoundGeocache.RemoveRange(database.FoundGeocache);
                database.SaveChanges();
            });

            Person p = new Person();
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }
            string path = dialog.FileName;
            List<List<String>> collection = new List<List<string>>();
            List<string> Objects = new List<string>();
            List<Person> peopleList = new List<Person>();
            List<string> foundValues = new List<string>();
            string[] lines = File.ReadAllLines(path).ToArray();
            foreach (var line in lines)
            {
                if (line != "")
                {
                    Objects.Add(line);
                    continue;
                }
                else
                {
                    collection.Add(Objects);
                    Objects = new List<string>();
                }
            }
            collection.Add(Objects);


            foreach (List<string> personLines in collection)
            {
                for (int i = 0; i < personLines.Count; i++)
                {
                    string[] values = personLines[i].Split('|').Select(v => v.Trim()).ToArray();

                    if (personLines[i].StartsWith("Found:"))
                    {
                        foundValues.Add(personLines[i]);
                    }

                    else if (values.Length > 5)
                    {

                        string firstName = values[0];
                        string lastName = values[1];
                        string country = values[2];
                        string city = values[3];
                        string streetName = values[4];
                        byte   streetNumber = Convert.ToByte( values[5]);
                        double latitude = double.Parse(values[6]);
                        double longtitude = double.Parse(values[7]);

                        p = new Person
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Country = country,
                            City = city,
                            StreetName = streetName,
                            StreetNumber = streetNumber,
                            Latitude = latitude,
                            Longitude = longtitude,
                        };
                        peopleList.Add(p);
                        database.Add(p);
                        database.SaveChanges();
                    }

                    else if (values.Length == 5)
                    {
                        int personId = int.Parse(values[0]);
                        double latitude = double.Parse(values[1]);
                        double longitude = double.Parse(values[2]);
                        string contents = values[3];
                        string message = values[4];

                        Geocache g = new Geocache
                        {
                            Latitude = latitude,
                            Longitude = longitude,
                            Contents = contents,
                            Message = message,
                        };
                        g.Person = p;
                        
                        database.Add(g);
                        database.SaveChanges();
                    }
                }
            }
            try
            {
                if (foundValues[0].StartsWith("Found:"))
                {
                    for (int i = 0; i < foundValues.Count; i++)
                    {
                        foundValues[i] = foundValues[i].Trim("Found: ".ToCharArray());
                        foundValues[i] = foundValues[i].Trim(" ".ToCharArray());
                        var indexes = foundValues[i].Split(',').ToArray();
                        var geoCaches = database.Geocache.ToList();

                        foreach (var geoS in indexes)
                        {
                            FoundGeocache fg = new FoundGeocache
                            {
                                Person = peopleList[i],
                                Geocache = geoCaches[int.Parse(geoS) - 1]
                            };
                            database.Add(fg);
                            database.SaveChanges();
                        }
                    }
                }
            }
            catch { }
            CreateMap();
        }
    }
}
