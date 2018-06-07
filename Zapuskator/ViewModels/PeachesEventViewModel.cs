using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using System.Windows;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Npgsql;
using Zapuskator.Framework;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
namespace Zapuskator.ViewModels
{
    
    public class PeachesEvent:PropertyChangedBase
    {
        private int _type;
        private TimeSpan _time;

        public PeachesEvent(TimeSpan time,int type)
        {
            Type = type;
            Time = time;
        }

        public int Type
        {
            get { return _type; }
            set
            {
                _type = value;
                NotifyOfPropertyChange();
            }
        }

        public TimeSpan Time
        {
            get { return _time; }
            set
            {
                _time = value;
                NotifyOfPropertyChange();
            }
        }
    }
    
    public class PeachesEventViewModel : Screen
    {
        private string connString = "Host=horton.elephantsql.com;Username=aoqvtpwp;Password=xwJmwdGfzn15Q8at6xaeCP1T-5pY366D;Database=aoqvtpwp";
        private readonly IEventAggregator _eventAggregator;
        public IDialogCoordinator _dialogCoordinator;
        private TimeSpan _selectedTime =DateTime.Now.TimeOfDay;
        private int _selectedEvent=0;
        private bool _clockwise;
        public string LicenseKey { get; set; }
        public List<Timer> Timers { get; set; }
        public ObservableCollection<PeachesEvent> Events { get; set; }

        public TimeSpan SelectedTime
        {
            get { return _selectedTime; }
            set
            {
                _selectedTime = value;
                NotifyOfPropertyChange();
            }
        }

        public int SelectedEvent
        {
            get { return _selectedEvent; }
            set
            {
                _selectedEvent = value;
                NotifyOfPropertyChange();
            }
        }

        public bool Clockwise
        {
            get { return _clockwise; }
            set
            {
                _clockwise = value;
                NotifyOfPropertyChange();
            }
        }

        public Notifier Notifier;
        public PeachesEventViewModel(IEventAggregator eventAggregator)
        {
            DateTime t = DateTime.Now;
            DateTime t2 = DateTime.Now;
            _eventAggregator = eventAggregator;
            _dialogCoordinator = DialogCoordinator.Instance;
            Events = new ObservableCollection<PeachesEvent>();
            Timers = new List<Timer>();
            var license = new LicenseHelper();
            LicenseKey=license.GetLicenseInfo();
            Notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.TopRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(6),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }

        public void Apply()
        {
            if (Services.Profiles.CurrentProfile.Peaches.Sync)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        conn.Open();

                        // Insert some data
                        using (var cmd = new NpgsqlCommand())
                        {
                            cmd.Connection = conn;
                            cmd.CommandText = "INSERT INTO events (appkey,type,server,date,clockwise) VALUES (@a,@b,@c,@d,@e)";
                            cmd.Parameters.AddWithValue("a", LicenseKey);
                            cmd.Parameters.AddWithValue("b", SelectedEvent);
                            cmd.Parameters.AddWithValue("c", Services.Profiles.CurrentProfile.Peaches.Server);
                            var date = DateTime.Now;
                            var neededdate = new DateTime(date.Year, date.Month, date.Day, SelectedTime.Hours, SelectedTime.Minutes, 0);
                            cmd.Parameters.AddWithValue("d", neededdate);
                            cmd.Parameters.AddWithValue("e", Clockwise);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    UpdateList();
                }
                catch (NpgsqlException)
                {
                    _eventAggregator.PublishOnUIThread("Ошибка подключения. Не удалось получить данные по сети.");
                }
            }
            else
            {
                UpdateList();
            }
        }

        public void TrySync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    // Retrieve all rows
                    using (var cmd = new NpgsqlCommand("SELECT type,date,clockwise FROM events WHERE server=" + Services.Profiles.CurrentProfile.Peaches.Server + " AND date>= now()::date + interval '5h' order by date desc,id desc limit 1", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    SelectedEvent = reader.GetInt32(0);
                                    SelectedTime = reader.GetDateTime(1).TimeOfDay;
                                    Clockwise = reader.GetBoolean(2);
                                }
                                UpdateList();
                            }
                            else
                            {
                                _eventAggregator.PublishOnUIThread("Увы. Сегодня еще никто не делился своим временем на выбранном сервере.");
                            }
                        
                            
                        }
                    }
                }

                
            }
            catch (NpgsqlException)
            {
                _eventAggregator.PublishOnUIThread("Ошибка подключения. Не удалось получить данные по сети.");
            }
        }

        public void UpdateList()
        {
            Events.Clear();
            foreach (var timer in Timers)
                timer.Stop();
            Timers.Clear();
            var tempdate = SelectedTime;
            var temptype = SelectedEvent;
            while (tempdate.Hours != 5)
            {
                tempdate = tempdate.Add(new TimeSpan(0, 43, 0));
                if (Clockwise)
                    temptype++;
                else
                    temptype--;
                Events.Add(new PeachesEvent(tempdate, Math.Abs(temptype % 3)));
                if (Services.Profiles.CurrentProfile.Peaches.Notifications)
                {
                    var nameOfEvent = temptype == 0 ? "Клыкари" :
                        temptype == 1 ? "Пустыня" : "Слоны";
                    var NowDate = DateTime.Now;
                    var timing = tempdate - new TimeSpan(0, 5, 0);
                    var time = new DateTime(NowDate.Year, NowDate.Month, NowDate.Day, timing.Hours, timing.Minutes, 0);
                    if (time > DateTime.Now)
                    {
                        var span = time - DateTime.Now;
                        var timer = new Timer {Interval = span.TotalMilliseconds, AutoReset = false};
                        timer.Elapsed += (sender, e) =>
                        {
                            _eventAggregator.PublishOnUIThread(DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss") + ":Через 5 минут будет событие на персиках: " + nameOfEvent);
                            Notifier.ShowInformation("Через 5 минут будет событие на персиках: " + nameOfEvent);
                        };
                        timer.Start();
                        Timers.Add(timer);
                    }
                }
            }
        }

        ~PeachesEventViewModel()
        {
            Notifier.Dispose();
        }
    }
}
