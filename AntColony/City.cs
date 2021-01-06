using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.RightsManagement;
using System.Text;

namespace AntColony
{
    public class City : INotifyPropertyChanged
    {
        public int CityNumber { get; set; }

        private string _cityName;
        public string CityName {
            get
            {
                return this._cityName;
            }
            set
            { 
                if(this._cityName != value)
                {
                    this._cityName = value;
                    this.NotifyPropertyChanged();
                }
            } 
        }
        private float _x;
        public float x
        {
            get { return _x; }
            set
            {
                if (this._x != value)
                {
                    this._x = value;
                    this.NotifyPropertyChanged();
                }
            }
        }
        private float _y;
        public float y
        {
            get { return _y; }
            set
            {
                if (this._y != value)
                {
                    this._y = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public City()
        {
            //if (CityName == "")
              //  this.CityName = this.CityNumber.ToString();
        }


        public float GetDistance(City city)
        {
            return (float)Math.Sqrt(
                Math.Pow(this.x - city.x, 2) + Math.Pow(this.y - city.y, 2)
                );
        }

        public override string ToString()
        {
            if(CityNumber.ToString() == CityName)
                return "City: " + CityNumber + ", at x: " + x + " y: " + y;
            else
                return "City: " + CityName + "(" + CityNumber + "), at x: " + x + " y: " + y;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName = "")
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

}
