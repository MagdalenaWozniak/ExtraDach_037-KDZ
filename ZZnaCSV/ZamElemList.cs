namespace ZZnaCSV
{
    internal class FSElemList //Rok	Miesiąc	Oddział	Nazwa	NIP	TypBadania	DataBadania	Pacjent	Pesel	Wiek	Lekarz kierujący	Badanie	Cena	StawkaVAT

    {
        public string Rok
        {
            set;
            get;

        }
        public string Miesiac
        {
            set;
            get;

        }
        public string Oddzial
        {
            set;
            get;
        }
        public string Nazwa
        {
            set;
            get;
        }
        public string Nip
        {
            set;
            get;
        }
        public string TypBadania
        {
            set;
            get;
        }

        public string DataBadania
        {
            set;
            get;
        }

        public string Pacjent
        {
            set;
            get;
        }
        public string Pesel
        {
            set;
            get;
        }

        public string Wiek
        {
            set;
            get;
        }

        public string Lekarz
        {
            set;
            get;
        }

        public string Badanie
        {
            set;
            get;
        }

        public string Cena
        {
            set;
            get;
        }

        public string StawkaVAT
        {
            set;
            get;
        }
        
            public static FSElemList FromCsv(string csvLine)////Rok	Miesiąc	Oddział	Nazwa	NIP	TypBadania	DataBadania	Pacjent	Pesel	Wiek	Lekarz kierujący	Badanie	Cena	StawkaVAT
            {
                string[] values = csvLine.Split(';');
                FSElemList csvlist = new FSElemList();
                csvlist.Rok = values[0];
                csvlist.Miesiac = values[1];
                csvlist.Oddzial = values[2];
                csvlist.Nazwa = values[3];
                csvlist.Nip = values[4];
                csvlist.TypBadania = values[5];
                csvlist.DataBadania = values[6];
                csvlist.Pacjent = values[7];
                csvlist.Pesel = values[8];
                csvlist.Wiek = values[9];
                csvlist.Lekarz = values[10];
                csvlist.Badanie = values[11];
                csvlist.Cena = values[12];
                csvlist.StawkaVAT = values[13];
                return csvlist;
            }
        }
    }

   /* sealed class UserMap : CsvClassMap<User>
    {
        public UserMap()
        {
            Map(m => m.Name).Index(0);
            Map(m => m.Surname).Index(1);
            Map(m => m.Age).Index(2);
        }
    }*/
