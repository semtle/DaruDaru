using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DaruDaru.Marumaru;
using Newtonsoft.Json;

namespace DaruDaru.Config.Entries
{
    internal class MarumaruEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        private string m_maruCode;
        public string MaruCode
        {
            get => this.m_maruCode;
            set
            {
                this.m_maruCode = value;
                this.Uri = DaruUriParser.Marumaru.GetUri(value);
            }
        }

        [JsonIgnore]
        public Uri Uri { get; private set; }

        public string Title { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string[] ArchiveCodes { get; set; }

        private DateTime m_lastUpdated;
        public DateTime LastUpdated
        {
            get => this.m_lastUpdated;
            set
            {
                this.m_lastUpdated = value;
                this.InvokePropertyChanged();
            }
        }
    }
}