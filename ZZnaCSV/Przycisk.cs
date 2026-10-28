using Hydra;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data;
using System;
using cdn_api;
using System.Collections.Generic;
using static KZD.Doki;
using System.Text;
using KZD;

[assembly: CallbackAssemblyDescription(Name: "ONE-037 KDZ",
                                           Description: "Procedura generowania dokumentów KDZ dla multikulek.",
                                           Author: "OneSupport",
                                           Version: "3.0",
                                           SystemVersion: "2023.2.0",
                                           Created: "28-12-2023")]
namespace KZD
{
    [SubscribeProcedure(Procedures.TrN_FZ, "KDZ")]
    public class DodajFakture : Callback
    {
        private long gidNumer = 0;
        private long gidTyp = 0;
        private long gidFirma = 0;
        private short gidLp = 0;

        // private Dane dane;
        public override void Init()
        {
            AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow));
            AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(Resize));
            // AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));
        }



        private bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
        {

            // ClaWindow Parent = GetWindow();
            //Rectangle WndBnd = GetWindow().Bounds;
            //zakladkaTabWewnetrzne = GetWindow().AllChildren["?TabFS"];

            //Bufor= Parent.AllChildren["?lEkr_TransakcjaDoBufora"];
            //Akronim = Parent.AllChildren["?Knt:Akronim"];
            //AddSubscription(false, Bufor.Id, Events.Accepted, new TakeEventDelegate(OnCloseWindow));
            gidNumer = TraNag.TrN_GIDNumer;
            gidFirma = TraNag.TrN_GIDFirma;
            gidLp = TraNag.TrN_GIDLp;
            gidTyp = TraNag.TrN_GIDTyp;

            AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));

            //MessageBox.Show("GID Numer: " + gidNumer, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //listID = GetWindow().AllChildren["?Handlowe"].Id;      //tu sprawdzic

            //pobrane z pola w zakładce Raporty
            return true;
        }

        public bool Resize(Procedures ProcId, int ControlId, Events Event)
        {

            return true;
        }

        public bool OnCloseWindow(Procedures ProcId, int ControlId, Events Event)
        {

            try
            {
                string connection_string = "";
                //MessageBox.Show("GID numer: " + gidNumer, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                int XLWersja = 20221;
                XLLoginInfo_20221 xlLoginInfo20221 = new XLLoginInfo_20221();
                xlLoginInfo20221.Wersja = XLWersja;
                xlLoginInfo20221.UtworzWlasnaSesje = 0;
                xlLoginInfo20221.Winieta = -1;
                xlLoginInfo20221.TrybWsadowy = 1;
                xlLoginInfo20221.ProgramID = "KZD";
                int sesja = 0;
                int num3 = cdn_api.cdn_api.XLLogin(xlLoginInfo20221, ref sesja);
                if (num3 != 0)
                {
                    MessageBox.Show("Błąd logowania! (API " + num3.ToString() + ")");
                }
                else
                {
                    //MessageBox.Show("-------- Rozpoczęcie działania programu --------");
                    //Console.WriteLine();
                    XLPolaczenieInfo_20221 polaczenieInfo20221 = new XLPolaczenieInfo_20221();
                    polaczenieInfo20221.Wersja = XLWersja;
                    if (cdn_api.cdn_api.XLPolaczenie(polaczenieInfo20221) != 0)
                    {
                        MessageBox.Show("Błąd połączenia do bazy SQL!");
                    }
                    SqlConnection connection = new SqlConnection();
                    try
                    {
                        // connection = new SqlConnection(polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")));
                        connection = new SqlConnection((polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", ""));
                        connection_string = (polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", "");

                        //MessageBox.Show(polaczenieInfo20220.ConnectString.Substring(polaczenieInfo20220.ConnectString.IndexOf("Data Source"), polaczenieInfo20220.ConnectString.Length - polaczenieInfo20220.ConnectString.IndexOf("Data Source")) + "; User ID = Wydruk; Password=S@msun6B@");
                    }
                    catch
                    {
                        MessageBox.Show("Błąd generowania połączenia " + polaczenieInfo20221.ConnectString);
                    }

                    connection.Open();
                    SqlCommand sqlCommand1 = new SqlCommand("select TrN_Stan from cdn.TraNag where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                    sqlCommand1.Parameters.Add("@GidNumer", SqlDbType.Int);
                    sqlCommand1.Parameters["@GidNumer"].Value = gidNumer;
                    sqlCommand1.Parameters.Add("@GidTyp", SqlDbType.Int);
                    sqlCommand1.Parameters["@GidTyp"].Value = gidTyp;
                    string stan = sqlCommand1.ExecuteScalar().ToString().Trim();

                    SqlCommand czyIstniejeQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE(Atk_id=265 or Atk_id=268 or Atk_id=267 or Atk_id=266 or Atk_id=280 or Atk_id=281) and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                "ELSE SELECT 'NIE'", connection);
                    czyIstniejeQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    czyIstniejeQuery.Parameters["@GidNumer"].Value = gidNumer;
                    czyIstniejeQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    czyIstniejeQuery.Parameters["@GidTyp"].Value = gidTyp;
                    string czyIstnieje = czyIstniejeQuery.ExecuteScalar().ToString();

                    SqlCommand czyCenaQuery = new SqlCommand("if exists (SELECT * FROM cdn.TraNag join cdn.TraElem on TrN_GIDTyp=TrE_GIDTyp and TrN_GIDNumer=TrE_GIDNumer where TRE_Cena = 0 and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp)" +
                                " SELECT 'TAK' else SELECT 'NIE'", connection);
                    czyCenaQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    czyCenaQuery.Parameters["@GidNumer"].Value = gidNumer;
                    czyCenaQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    czyCenaQuery.Parameters["@GidTyp"].Value = gidTyp;
                    string czyCenaZero = czyCenaQuery.ExecuteScalar().ToString();


                    SqlCommand iloscQuery = new SqlCommand("SELECT (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp and Twr_Typ != 4) - " +
                                                                " (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE (Atk_id=266 or Atk_id=280)  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp)", connection);
                    iloscQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    iloscQuery.Parameters["@GidNumer"].Value = gidNumer;
                    iloscQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    iloscQuery.Parameters["@GidTyp"].Value = gidTyp;
                    int ilosc = Convert.ToInt32(iloscQuery.ExecuteScalar().ToString());

                    //MessageBox.Show("Stan: " + stan);

                    if (czyIstnieje == "TAK" && stan == "3" && ilosc > 0 && czyCenaZero=="NIE")// || (stan=="2" && Bufor.CheckedRaw=="0")))
                    {

                        //MessageBox.Show("Istnieje atrybut", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        SqlCommand akronimQuery = new SqlCommand("SELECT  knt_Akronim from cdn.TraNAg join cdn.KntKarty on TrN_KntNumer = Knt_GIDNumer and TrN_KntTyp = Knt_GIDTyp where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        akronimQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        akronimQuery.Parameters["@GidNumer"].Value = gidNumer;
                        akronimQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        akronimQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string akronim = akronimQuery.ExecuteScalar().ToString();


                        SqlCommand seriaQuery = new SqlCommand("SELECT TrN_TrNSeria from cdn.TraNAg where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        seriaQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        seriaQuery.Parameters["@GidNumer"].Value = gidNumer;
                        seriaQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        seriaQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string seria = seriaQuery.ExecuteScalar().ToString();

                        SqlCommand frsQuery = new SqlCommand("SELECT TrN_FrsID from cdn.TraNAg where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        frsQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        frsQuery.Parameters["@GidNumer"].Value = gidNumer;
                        frsQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        frsQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string frs = frsQuery.ExecuteScalar().ToString();


                        SqlCommand numerDokQuery = new SqlCommand("SELECT cdn.NumerDokumentu(TrN_GIDTyp, TrN_SpiTyp, TrN_TrNTyp, TrN_TrNNumer, TrN_TrNRok, TrN_TrNSeria, TrN_TrNMiesiac) from cdn.TraNAg  where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        numerDokQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        numerDokQuery.Parameters["@GidNumer"].Value = gidNumer;
                        numerDokQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        numerDokQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string numerDok = numerDokQuery.ExecuteScalar().ToString();

                        

                        decimal zarzadWartosc = 0;
                        decimal oddzialWartosc = 0;



                        SqlCommand czyBonusZarzaduQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE (Atk_id=265 or Atk_id=281) and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                "ELSE SELECT 'NIE'", connection);
                        czyBonusZarzaduQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyBonusZarzaduQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyBonusZarzaduQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyBonusZarzaduQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyBonusZarzadu = czyBonusZarzaduQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        if (czyBonusZarzadu == true)
                        {
                            zarzadWartosc = DodDok.WyliczZarzad(connection,(int) gidNumer, (int)gidTyp);
                            //MessageBox.Show("Zarząd wartość: " + zarzadWartosc);
                        }

                        SqlCommand czyZyskOddzialuQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE Atk_id=268  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyZyskOddzialuQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyZyskOddzialuQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyZyskOddzialuQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyZyskOddzialuQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyZyskOddzialu = czyZyskOddzialuQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        if (czyZyskOddzialu == true)
                        {
                            oddzialWartosc = DodDok.WyliczOddzial(connection, (int)gidNumer, (int)gidTyp);
                            //MessageBox.Show("ZyskOddziału wartość: " + oddzialWartosc);
                        }

                        SqlCommand czyTransportQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE Atk_id=267  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyTransportQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyTransportQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyTransportQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyTransportQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyTransport = czyTransportQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        /*if (czyTransport == true)
                        {
                            transportWartosc = DodDok.WyliczTransport(connection, (int)gidTyp,  (int) gidNumer);
                            //MessageBox.Show("Transport wartość: " + transportWartosc);
                        }*/

                        SqlCommand czyPaletyQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE Atk_id=266  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyPaletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyPaletyQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyPaletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyPaletyQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyPalety = czyPaletyQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        SqlCommand czyPaletyZlQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE Atk_id=280  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyPaletyZlQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyPaletyZlQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyPaletyZlQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyPaletyZlQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyPaletyZl = czyPaletyZlQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        SqlCommand iloscBonusZarzaduQuery = new SqlCommand("SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                 " WHERE (Atk_id=265 or Atk_id=281)  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                        iloscBonusZarzaduQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        iloscBonusZarzaduQuery.Parameters["@GidNumer"].Value = gidNumer;
                        iloscBonusZarzaduQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        iloscBonusZarzaduQuery.Parameters["@GidTyp"].Value = gidTyp;
                        int iloscBonusZarzadu = Convert.ToInt32(iloscBonusZarzaduQuery.ExecuteScalar().ToString());


                        SqlCommand iloscZyskOddzialuQuery = new SqlCommand("SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                 " WHERE Atk_id=268   and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                        iloscZyskOddzialuQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        iloscZyskOddzialuQuery.Parameters["@GidNumer"].Value = gidNumer;
                        iloscZyskOddzialuQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        iloscZyskOddzialuQuery.Parameters["@GidTyp"].Value = gidTyp;
                        int iloscZyskOddzialu = Convert.ToInt32(iloscZyskOddzialuQuery.ExecuteScalar().ToString());








                        XLZwiazanyDokInfo_20221 zwiazany = new XLZwiazanyDokInfo_20221();
                        zwiazany.Wersja = XLWersja;
                        zwiazany.GIDTyp = (int)gidTyp;
                        zwiazany.GIDNumer = (int)gidNumer;
                        int blad = cdn_api.cdn_api.XLDokumentyZwiazane(zwiazany);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania dokumentu związanego: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        int l = 0;
                       

                        if (czyTransport == true || czyPalety == true ||czyPaletyZl==true)
                        {
                            if (czyBonusZarzadu == true && czyZyskOddzialu == true )
                            {
                                if (iloscBonusZarzadu == iloscZyskOddzialu && (zarzadWartosc >= 0 && oddzialWartosc >= 0) )
                                {
                                    if (iloscBonusZarzadu == ilosc )
                                    {
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety,czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety,czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                }
                                else
                                {
                                    if (iloscBonusZarzadu == ilosc  && zarzadWartosc>=0)
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                       
                                    }
                                    else if (iloscZyskOddzialu == ilosc && oddzialWartosc>=0)
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety,czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false,false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety,czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                }
                            }
                            else
                            {
                                if (czyBonusZarzadu == true)
                                {
                                    if (iloscBonusZarzadu == ilosc && zarzadWartosc>=0)
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false,false,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                }
                                else if (czyZyskOddzialu == true)
                                {
                                    if (iloscZyskOddzialu == ilosc && oddzialWartosc >=0)
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                        
                                    }

                                }
                                else
                                {
                                    DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                    
                                }
                            }

                        }
                        else if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                        {
                            if (iloscBonusZarzadu == iloscZyskOddzialu && ((zarzadWartosc >= 0 && oddzialWartosc >= 0) || (zarzadWartosc < 0 && oddzialWartosc < 0)))
                            {
                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                
                            }
                            else
                            {
                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                
                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                                
                            }
                        }
                        else
                        {
                            DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, frs);
                            
                        }






                        XLZwiazanyDokInfo_20221 rozwiazany = new XLZwiazanyDokInfo_20221();
                        rozwiazany.Wersja = XLWersja;
                        rozwiazany.Akcja = -1;
                        blad = cdn_api.cdn_api.XLDokumentyZwiazane(rozwiazany);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd usuwania dokumentu związanego: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }


                        //DodDok.SprawdzZasoby();

                            //KDZty.Clear();

                        

                    }
                    else if (czyCenaZero == "TAK")
                    {
                        MessageBox.Show("W fakturze występują ceny 0zł. Anulowano dodawanie KDZ ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    }

                    connection.Close();
                }

                int blad2 = cdn_api.cdn_api.XLLogout(sesja);
                if (blad2 != 0)
                {
                    MessageBox.Show("Błąd wylogowywania: " + blad2, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Błąd funkcji onCloseWindow: " + e.Message, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

        }

   
        public string ConnectToSQL(string connection_string, string query)
        {
            string str = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, "Query");
                    if (dataSet.Tables.Count == 0)
                    {
                        return "";
                    }
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {
                        str = row[0].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                str = str + "Bład SQL:" + ex.ToString() + "\n";
            }

            return str;
        }

        #region
        public override void Cleanup()
        {
        }


        #endregion



    }
   


    [SubscribeProcedure(Procedures.ListaDokumentow, "KDZ")]
    public class KDZPrzycisk : Callback
    {
        private ClaWindow RapButton;
        private ClaWindow zakladkaTabWewnetrzne;
        private ClaWindow filtrBtn;

        string XLSQLDatabase = "";
        int listID = 0;
        string connectionString = "";
        private List<ListaDok> doki = new List<ListaDok>();

        public override void Cleanup()
        {
        }

        // private Dane dane;
        public override void Init()
        {
            AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow));
            AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(Resize));
            // AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));
        }

        private bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
        {

            ClaWindow Parent = GetWindow();
            Rectangle WndBnd = GetWindow().Bounds;
            zakladkaTabWewnetrzne = GetWindow().AllChildren["?TabHan"];
            RapButton = zakladkaTabWewnetrzne.Children.Add(ControlTypes.button);
            RapButton.Visible = true;
            RapButton.TextRaw = "KDZ";
            RapButton.ToolTipRaw = "Dodaj dokumenty KDZ do dokumentów FZ";
            RapButton.OnAfterAccepted += new TakeEventDelegate(BtnOnMouseDown);

            filtrBtn = GetWindow().AllChildren["?Cancel"];                            //pobiera współrzędne przycisku zapisz
            RapButton.Bounds = new Rectangle(filtrBtn.Bounds.X, filtrBtn.Bounds.Y - 20, 20, 15);
            listID = GetWindow().AllChildren["?Handlowe"].Id;      //tu sprawdzic

            //pobrane z pola w zakładce Raporty
            return true;
        }

        public bool Resize(Procedures ProcId, int ControlId, Events Event)
        {
            filtrBtn = GetWindow().AllChildren["?Cancel"];                            //pobiera współrzędne przycisku zapisz
            RapButton.Bounds = new Rectangle(filtrBtn.Bounds.X, filtrBtn.Bounds.Y - 20, 20, 15);
            return true;
        }



        private bool BtnOnMouseDown(Procedures ProcId, int ControlId, Events Event)
        {
            try
            {

                // Application.EnableVisualStyles();
                // Application.SetCompatibleTextRenderingDefault(false);

                ADODB._Recordset recordset = Runtime.WindowController.GetQueueMarked((int)ProcId, listID, GetCallbackThread());
                if (recordset != null)
                {
                    // MessageBox.Show(Convert.ToString(listID), "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    ZbieranieFZ(recordset);
                }

                if (doki.Count > 0)
                {
                    foreach (ListaDok dok in doki)
                    {

                        try
                        {

                            int gidTyp = dok.GIDTyp;
                            int gidNumer = dok.GIDNumer;
                            string connection_string = "";
                            //MessageBox.Show("GID numer: " + gidNumer, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            int XLWersja = 20221;
                            XLLoginInfo_20221 xlLoginInfo20221 = new XLLoginInfo_20221();
                            xlLoginInfo20221.Wersja = XLWersja;
                            xlLoginInfo20221.UtworzWlasnaSesje = 0;
                            xlLoginInfo20221.Winieta = -1;
                            xlLoginInfo20221.TrybWsadowy = 1;
                            xlLoginInfo20221.ProgramID = "KZD";
                            int sesja = 0;
                            int num3 = cdn_api.cdn_api.XLLogin(xlLoginInfo20221, ref sesja);
                            if (num3 != 0)
                            {
                                MessageBox.Show("Błąd logowania! (API " + num3.ToString() + ")");
                            }
                            else
                            {
                                //MessageBox.Show("-------- Rozpoczęcie działania programu --------");
                                //Console.WriteLine();
                                XLPolaczenieInfo_20221 polaczenieInfo20221 = new XLPolaczenieInfo_20221();
                                polaczenieInfo20221.Wersja = XLWersja;
                                if (cdn_api.cdn_api.XLPolaczenie(polaczenieInfo20221) != 0)
                                {
                                    MessageBox.Show("Błąd połączenia do bazy SQL!");
                                }
                                SqlConnection connection = new SqlConnection();
                                try
                                {
                                    // connection = new SqlConnection(polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")));
                                    connection = new SqlConnection((polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", ""));
                                    connection_string = (polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", "");

                                    //MessageBox.Show(polaczenieInfo20220.ConnectString.Substring(polaczenieInfo20220.ConnectString.IndexOf("Data Source"), polaczenieInfo20220.ConnectString.Length - polaczenieInfo20220.ConnectString.IndexOf("Data Source")) + "; User ID = Wydruk; Password=S@msun6B@");
                                }
                                catch
                                {
                                    MessageBox.Show("Błąd generowania połączenia " + polaczenieInfo20221.ConnectString);
                                }

                                connection.Open();
                                SqlCommand sqlCommand1 = new SqlCommand("select TrN_Stan from cdn.TraNag where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                                sqlCommand1.Parameters.Add("@GidNumer", SqlDbType.Int);
                                sqlCommand1.Parameters["@GidNumer"].Value = gidNumer;
                                sqlCommand1.Parameters.Add("@GidTyp", SqlDbType.Int);
                                sqlCommand1.Parameters["@GidTyp"].Value = gidTyp;
                                string stan = sqlCommand1.ExecuteScalar().ToString().Trim();


                                SqlCommand sqlCommand = new SqlCommand("select TrN_SpiTyp from cdn.TraNag where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                                sqlCommand.Parameters.Add("@GidNumer", SqlDbType.Int);
                                sqlCommand.Parameters["@GidNumer"].Value = gidNumer;
                                sqlCommand.Parameters.Add("@GidTyp", SqlDbType.Int);
                                sqlCommand.Parameters["@GidTyp"].Value = gidTyp;
                                int spiTyp = Convert.ToInt32(sqlCommand.ExecuteScalar().ToString().Trim());

                                int iloscDok = 0;

                                SqlCommand numerDokQuery = new SqlCommand("SELECT cdn.NumerDokumentu(TrN_GIDTyp, TrN_SpiTyp, TrN_TrNTyp, TrN_TrNNumer, TrN_TrNRok, TrN_TrNSeria, TrN_TrNMiesiac) from cdn.TraNAg  where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                                numerDokQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                numerDokQuery.Parameters["@GidNumer"].Value = gidNumer;
                                numerDokQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                numerDokQuery.Parameters["@GidTyp"].Value = gidTyp;
                                string numerDok = numerDokQuery.ExecuteScalar().ToString();

                                SqlCommand frsQuery = new SqlCommand("SELECT TrN_FrsID from cdn.TraNAg where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                                frsQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                frsQuery.Parameters["@GidNumer"].Value = gidNumer;
                                frsQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                frsQuery.Parameters["@GidTyp"].Value = gidTyp;
                                string frs = frsQuery.ExecuteScalar().ToString();

                                if (spiTyp==1521)
                                { 
                                SqlCommand czyIstniejeQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                            " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                            " WHERE(Atk_id=265 or Atk_id=268 or Atk_id=267 or Atk_id=266 or Atk_id=280 or Atk_id=281) and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                            "ELSE SELECT 'NIE'", connection);
                                czyIstniejeQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                czyIstniejeQuery.Parameters["@GidNumer"].Value = gidNumer;
                                czyIstniejeQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                czyIstniejeQuery.Parameters["@GidTyp"].Value = gidTyp;
                                string czyIstnieje = czyIstniejeQuery.ExecuteScalar().ToString();

                                    SqlCommand czyCenaQuery = new SqlCommand("if exists (SELECT * FROM cdn.TraNag join cdn.TraElem on TrN_GIDTyp=TrE_GIDTyp and TrN_GIDNumer=TrE_GIDNumer where TRE_Cena = 0 and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp)" +
                                                                                " SELECT 'TAK' else SELECT 'NIE'", connection);
                                    czyCenaQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                    czyCenaQuery.Parameters["@GidNumer"].Value = gidNumer;
                                    czyCenaQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                    czyCenaQuery.Parameters["@GidTyp"].Value = gidTyp;
                                    string czyCenaZero = czyCenaQuery.ExecuteScalar().ToString();


                                    SqlCommand iloscQuery = new SqlCommand("SELECT (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                            " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp and Twr_Typ != 4) - " +
                                                                            " (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                                            " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                            " WHERE (Atk_id=266 or Atk_id=280)  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp)", connection);
                                iloscQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                iloscQuery.Parameters["@GidNumer"].Value = gidNumer;
                                iloscQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                iloscQuery.Parameters["@GidTyp"].Value = gidTyp;
                                int ilosc = Convert.ToInt32(iloscQuery.ExecuteScalar().ToString());

                                    if(ilosc==0)
                                    {
                                        iloscQuery = new SqlCommand("if (SELECT STRING_AGG(CONVERT( varchar(15), Tre_GIDNumer) +CONVERT( varchar(5), Tre_GIDLp),',') from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                            " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp and Twr_Typ != 4) = " +
                                                                            " (SELECT STRING_AGG(CONVERT( varchar(15), Tre_GIDNumer) +CONVERT( varchar(5), Tre_GIDLp),',') from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                                            " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                            " WHERE (Atk_id=266 or Atk_id=280)  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 0 else SELECT 1", connection);
                                        ilosc = Convert.ToInt32(iloscQuery.ExecuteScalar().ToString());
                                    }

                                    // MessageBox.Show("Stan: " + stan);

                                    if (czyIstnieje == "TAK" && stan == "3" && ilosc > 0 && czyCenaZero=="NIE")// || (stan=="2" && Bufor.CheckedRaw=="0")))
                                    {

                                        //MessageBox.Show("Istnieje atrybut", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        SqlCommand akronimQuery = new SqlCommand("SELECT  knt_Akronim from cdn.TraNAg join cdn.KntKarty on TrN_KntNumer = Knt_GIDNumer and TrN_KntTyp = Knt_GIDTyp where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                                        akronimQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        akronimQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        akronimQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        akronimQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        string akronim = akronimQuery.ExecuteScalar().ToString();

                                        SqlCommand seriaQuery = new SqlCommand("SELECT TrN_TrNSeria from cdn.TraNAg where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                                        seriaQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        seriaQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        seriaQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        seriaQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        string seria = seriaQuery.ExecuteScalar().ToString();




                                        decimal zarzadWartosc = 0;
                                        decimal oddzialWartosc = 0;
                                        int dzisiaj = 0;




                                        SqlCommand czyBonusZarzaduQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                                " WHERE (Atk_id=265 or Atk_id=281)  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                                "ELSE SELECT 'NIE'", connection);
                                        czyBonusZarzaduQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        czyBonusZarzaduQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        czyBonusZarzaduQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        czyBonusZarzaduQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        bool czyBonusZarzadu = czyBonusZarzaduQuery.ExecuteScalar().ToString().Trim() == "TAK";

                                        if (czyBonusZarzadu == true)
                                        {
                                            zarzadWartosc = DodDok.WyliczZarzad(connection, gidNumer, gidTyp);
                                           // MessageBox.Show("Zarząd wartość: " + zarzadWartosc);
                                        }

                                        SqlCommand czyZyskOddzialuQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                               " WHERE Atk_id=268  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                               "ELSE SELECT 'NIE'", connection);
                                        czyZyskOddzialuQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        czyZyskOddzialuQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        czyZyskOddzialuQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        czyZyskOddzialuQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        bool czyZyskOddzialu = czyZyskOddzialuQuery.ExecuteScalar().ToString().Trim() == "TAK";

                                        if(czyZyskOddzialu==true)
                                        {
                                            oddzialWartosc = DodDok.WyliczOddzial(connection, gidNumer, gidTyp);
                                           // MessageBox.Show("ZyskOddziału wartość: " + oddzialWartosc);
                                        }

                                        SqlCommand czyTransportQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                               " WHERE Atk_id=267  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                               "ELSE SELECT 'NIE'", connection);
                                        czyTransportQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        czyTransportQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        czyTransportQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        czyTransportQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        bool czyTransport = czyTransportQuery.ExecuteScalar().ToString().Trim() == "TAK";

                                       /* if(czyTransport==true)
                                        {
                                            transportWartosc = DodDok.WyliczTransport(connection, gidTyp, gidNumer);
                                            MessageBox.Show("Transport wartość: " + transportWartosc);
                                        }*/

                                        SqlCommand czyPaletyQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                               " WHERE Atk_id=266  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                               "ELSE SELECT 'NIE'", connection);
                                        czyPaletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        czyPaletyQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        czyPaletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        czyPaletyQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        bool czyPalety = czyPaletyQuery.ExecuteScalar().ToString().Trim() == "TAK";

                                        SqlCommand czyPaletyZlQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                               " WHERE Atk_id=280  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                               "ELSE SELECT 'NIE'", connection);
                                        czyPaletyZlQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        czyPaletyZlQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        czyPaletyZlQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        czyPaletyZlQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        bool czyPaletyZl = czyPaletyZlQuery.ExecuteScalar().ToString().Trim() == "TAK";

                                        SqlCommand iloscBonusZarzaduQuery = new SqlCommand("SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                                 " WHERE (Atk_id=265 or Atk_id=281)  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                                        iloscBonusZarzaduQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        iloscBonusZarzaduQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        iloscBonusZarzaduQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        iloscBonusZarzaduQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        int iloscBonusZarzadu = Convert.ToInt32(iloscBonusZarzaduQuery.ExecuteScalar().ToString());


                                        SqlCommand iloscZyskOddzialuQuery = new SqlCommand("SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                                 " WHERE Atk_id=268   and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                                        iloscZyskOddzialuQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                        iloscZyskOddzialuQuery.Parameters["@GidNumer"].Value = gidNumer;
                                        iloscZyskOddzialuQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                        iloscZyskOddzialuQuery.Parameters["@GidTyp"].Value = gidTyp;
                                        int iloscZyskOddzialu = Convert.ToInt32(iloscZyskOddzialuQuery.ExecuteScalar().ToString());




                                        XLZwiazanyDokInfo_20221 zwiazany = new XLZwiazanyDokInfo_20221();
                                        zwiazany.Wersja = XLWersja;
                                        zwiazany.GIDTyp = (int)gidTyp;
                                        zwiazany.GIDNumer = (int)gidNumer;
                                        int blad2 = cdn_api.cdn_api.XLDokumentyZwiazane(zwiazany);
                                        if (blad2 != 0)
                                        {
                                            MessageBox.Show("Błąd dodawania dokumentu związanego: " + blad2, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }


                                        

                                        if (czyTransport == true || czyPalety == true || czyPaletyZl==true)
                                        {
                                            if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                                            {
                                                if (iloscBonusZarzadu == iloscZyskOddzialu && (zarzadWartosc >= 0 && oddzialWartosc >= 0))
                                                {
                                                    if (iloscBonusZarzadu == ilosc)
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                       
                                                    }
                                                    else
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }
                                                }
                                                else
                                                {
                                                    if (iloscBonusZarzadu == ilosc && zarzadWartosc >= 0)
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                       
                                                    }
                                                    else if (iloscZyskOddzialu == ilosc && oddzialWartosc >= 0)
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }
                                                    else
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false,  connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (czyBonusZarzadu == true)
                                                {
                                                    if (iloscBonusZarzadu == ilosc && zarzadWartosc >= 0)
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }
                                                    else
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }
                                                }
                                                else if (czyZyskOddzialu == true)
                                                {
                                                    if (iloscZyskOddzialu == ilosc && oddzialWartosc >= 0)
                                                    {

                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }
                                                    else
                                                    {
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                        
                                                    }

                                                }
                                                else
                                                {
                                                    DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                    
                                                }
                                            }

                                        }
                                        else if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                                        {
                                            if (iloscBonusZarzadu == iloscZyskOddzialu && ((zarzadWartosc >= 0 && oddzialWartosc >= 0) || (zarzadWartosc < 0 && oddzialWartosc < 0)))
                                            {
                                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                               
                                            }
                                            else
                                            {
                                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                
                                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                                
                                            }
                                        }
                                        else
                                        {
                                            DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, gidNumer, gidTyp, ref iloscDok, seria, frs);
                                            
                                        }


                                        
                                    }
                                    else if (czyCenaZero == "TAK")
                                    {
                                        MessageBox.Show("W fakturze występują ceny 0zł. Anulowano dodawanie KDZ ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    }


                                }
                                else if (spiTyp == 0)
                                {
                                    string gidNumery = "";
                                    string PZtyQuery = "SELECT TrN_GIDTyp, TrN_GIDFirma ,TrN_GIDNumer,TrN_GIDLp  FROM cdn.TraNag WHERE TrN_GIDTyp = 1489 and TrN_SpiTyp = 1521 and TrN_SpiNumer = " + gidNumer;
                                    List<ListaDok> PZty = SelectListDoki(PZtyQuery, "cdn.TraNag", connection_string);

                                    for (int i = 0; i < PZty.Count; i++)
                                    {
                                        gidNumery += PZty[i].GIDNumer.ToString();
                                        if (i != (PZty.Count - 1))
                                        {
                                            gidNumery += ",";
                                        }
                                    }

                                   // MessageBox.Show("gidNumery: " + gidNumery);
                                    DodDok.WykonajKDZ(connection_string, gidNumery, stan, gidNumer, gidTyp, PZty, connection, XLWersja, sesja, numerDok, ref iloscDok, frs);

                                    PZty.Clear();
                                }
                                else
                                {
                                    string gidNumery = "";
                                    string PZtyQuery = "SELECT TrS_SpiTyp, TrS_SpiFirma ,TrS_SpiNumer,TrS_SpiLp  FROM cdn.TraSElem WHERE TrS_GIDTyp = 1521 and TrS_SpiTyp = 1489 and TrS_GIDNumer = " + gidNumer
                                                        + " group by TrS_SpiLp, TrS_SpiNumer, TrS_SpiFirma, TrS_SpiTyp"; 
                                   // MessageBox.Show("PZty:\n" + PZtyQuery);
                                    List<ListaDok>PZty = SelectListDoki(PZtyQuery, "cdn.TraSElem", connection_string);


                                    gidNumery += "(";
                                    for (int i = 0; i < PZty.Count; i++)
                                    {
                                        gidNumery += " (TrE_GIDNumer =" + PZty[i].GIDNumer.ToString() + " and TrE_GIDLp=" + PZty[i].GIDLp.ToString() + ") ";

                                        if (i != (PZty.Count - 1))
                                        {
                                            gidNumery += " or ";
                                        }
                                    }

                                    gidNumery += ")";
                                    
                                    DodDok.WykonajKDZMalySpinacz(connection_string, gidNumery, stan, gidNumer, gidTyp, PZty, connection, XLWersja, sesja, numerDok, ref iloscDok, frs);
                                    PZty.Clear();

                                }




                                MessageBox.Show("Wygenerowano " + iloscDok + " KDZ dla dokumentu " + numerDok, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                XLZwiazanyDokInfo_20221 rozwiazany = new XLZwiazanyDokInfo_20221();
                                rozwiazany.Wersja = XLWersja;
                                rozwiazany.Akcja = -1;
                                int blad = cdn_api.cdn_api.XLDokumentyZwiazane(rozwiazany);
                                if (blad != 0)
                                {
                                    MessageBox.Show("Błąd usuwania dokumentu związanego: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }



                                blad = cdn_api.cdn_api.XLLogout(sesja);
                                if (blad != 0)
                                {
                                    MessageBox.Show("Błąd wylogowywania: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }

                                connection.Close();
                            }

                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Błąd funkcji onButtonMouse: " + e.Message, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return false;
                        }

                    }

                    doki.Clear();
                }






                return true;


            }
            catch (Exception e)
            {
                MessageBox.Show("Bład aplikacji: " + e.Message, "Komunikat",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }


        #region kom

        /*public void dodajDokument(int sesja, int XLWersja, string akronim, string numerDok, bool bonusZarzadu, bool zyskOddzialu, bool transport, bool palety, string connection_string, int trybZam, int gidNumer, int gidTyp, ref int iloscDok, string seria)

        {
            int nrDok = 0;
            string typ = (bonusZarzadu == true ? " Bonus zarządu " : "") + (zyskOddzialu == true ? " Zysk handlowy oddziału " : "") + (transport == true ? " Transport " : "") + (palety == true ? " Palety " : "");



            SqlConnection connection = new SqlConnection(connection_string);
            connection.Open();


            SqlCommand czyIstniejeDok = new SqlCommand("if exists (SELECT * FROM [CDN].[TrNOpisy] WHERE [TnO_Opis] = @opis) SELECT 'TAK' else SELECT 'NIE'", connection);
            czyIstniejeDok.Parameters.Add("@opis", SqlDbType.VarChar);
            czyIstniejeDok.Parameters["@opis"].Value = akronim + " " + numerDok + " - " + typ;
            string istniejeDok = czyIstniejeDok.ExecuteScalar().ToString();


            if (istniejeDok.Trim() == "NIE")
            {

                XLDokumentNagInfo_20221 dok = new XLDokumentNagInfo_20221();
                dok.Wersja = XLWersja;
                dok.Typ = 1232;
                dok.Zwiazane = 1;
                dok.PrzyczynaKorekty = "HAN";
                dok.Seria = seria;
                dok.Tryb = 2;
                dok.Opis = akronim + " " + numerDok + " - " + typ;
                int blad = cdn_api.cdn_api.XLNowyDokument(sesja, ref nrDok, dok);
                if (blad != 0)
                {
                    MessageBox.Show("Błąd dodawania dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                iloscDok++;
                #region usuwanie pozycji
                if (bonusZarzadu)
                {
                    string pozycjeQuery = "SELECt Tre_GIDLp FROM cdn.TraElem where TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + " AND Tre_GIDLp NOT IN ( SELECT Tre_GIDLp from cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  " +
                                            " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                   " WHERE AtK_Nazwa = 'Bonus zarządu [%]' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
                    List<int> pozycje = SelectListRowColumn(pozycjeQuery, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycje)
                    {
                        XLUsunElemInfo_20221 poz = new XLUsunElemInfo_20221();
                        poz.Pozycja = lp;
                        poz.Wersja = XLWersja;
                        blad = cdn_api.cdn_api.XLUsunPozycje(nrDok, poz);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd usuwania pozycji nie należących do Bonus Zarządu dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }


                if (palety)
                {
                    string pozycjeQuery = "SELECt Tre_GIDLp FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                   " WHERE AtK_Nazwa = 'Palety' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer;
                    List<int> pozycje = SelectListRowColumn(pozycjeQuery, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycje)
                    {
                        XLUsunElemInfo_20221 poz = new XLUsunElemInfo_20221();
                        poz.Pozycja = lp;
                        poz.Wersja = XLWersja;
                        blad = cdn_api.cdn_api.XLUsunPozycje(nrDok, poz);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd usuwania pozycji Palety dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }


                }

                if (zyskOddzialu)
                {
                    string pozycjeQuery = "SELECt Tre_GIDLp FROM cdn.TraElem where TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + " AND Tre_GIDLp NOT IN ( SELECT Tre_GIDLp from cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  " +
                                            " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                   " WHERE AtK_Nazwa = 'Zysk handlowy oddziału [%]' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
                    List<int> pozycje = SelectListRowColumn(pozycjeQuery, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycje)
                    {
                        XLUsunElemInfo_20221 poz = new XLUsunElemInfo_20221();
                        poz.Pozycja = lp;
                        poz.Wersja = XLWersja;
                        blad = cdn_api.cdn_api.XLUsunPozycje(nrDok, poz);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd usuwania pozycji nie należących do Zysk handlowy oddziału dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }


                #endregion


                int lp_koszt = 1;
                int lp_transport = 0;
                int lp_palety = 0;
                int lp_zarzad = 0;
                int lp_oddzial = 0;
                int data = 0;
                decimal paletyWartosc = 0;
                decimal transportWartosc = 0;
                decimal zarzadWartosc = 0;
                decimal oddzialWartosc = 0;
                int dzisiaj = 0;






                #region dodawanie kosztów


                if (bonusZarzadu || zyskOddzialu)
                {
                    SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                            " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                    dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                    dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                    data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                    if (data == 0)
                    {
                        /*SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                        data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                        data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                        data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());*/
        /* data = dok.Data;
     }

 }


 if (transport)
 {
     SqlCommand selectTransportWartosc = new SqlCommand("SELECt SUM(ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                    + " WHERE AtK_Nazwa = 'Transport' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
     selectTransportWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
     selectTransportWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
     selectTransportWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
     selectTransportWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
     transportWartosc = Convert.ToDecimal(selectTransportWartosc.ExecuteScalar().ToString());

     SqlCommand updateTransport = new SqlCommand("update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1 where KDD_GIDNumer=@GidNumer  and KDD_GIDTyp = @GidTyp and KDD_GIDLp=1", connection);
     //, KDD_Wartosc=@Wartosc
     updateTransport.Parameters.Add("@GidNumer", SqlDbType.Int);
     updateTransport.Parameters["@GidNumer"].Value = dok.GIDNumer;
     updateTransport.Parameters.Add("@GidTyp", SqlDbType.Int);
     updateTransport.Parameters["@GidTyp"].Value = dok.GIDTyp;
     // updateTransport.Parameters.Add("@Wartosc", SqlDbType.Decimal);
     //updateTransport.Parameters["@Wartosc"].Value = transportWartosc;
     updateTransport.ExecuteNonQuery();

     lp_transport = lp_koszt;
     lp_koszt++;

 }
 else
 {
     SqlCommand deleteTransport = new SqlCommand("delete from cdn.KosztyDodDok  where KDD_GIDNumer=@GidNumer  and KDD_GIDTyp = @GidTyp ", connection);
     //, KDD_Wartosc=@Wartosc
     deleteTransport.Parameters.Add("@GidNumer", SqlDbType.Int);
     deleteTransport.Parameters["@GidNumer"].Value = dok.GIDNumer;
     deleteTransport.Parameters.Add("@GidTyp", SqlDbType.Int);
     deleteTransport.Parameters["@GidTyp"].Value = dok.GIDTyp;
     // updateTransport.Parameters.Add("@Wartosc", SqlDbType.Decimal);
     //updateTransport.Parameters["@Wartosc"].Value = transportWartosc;
     deleteTransport.ExecuteNonQuery();
 }

 if (palety)
 {

     SqlCommand selectPaletyWartosc = new SqlCommand("SELECt SUM(ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                    + " WHERE AtK_Nazwa = 'Palety' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
     selectPaletyWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
     selectPaletyWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
     selectPaletyWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
     selectPaletyWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
     paletyWartosc = Convert.ToDecimal(selectPaletyWartosc.ExecuteScalar().ToString());



     SqlCommand insertPalety = new SqlCommand("INSERT INTO [CDN].[KosztyDodDok] ([KDD_GIDTyp] ,[KDD_GIDFirma] ,[KDD_GIDNumer] ,[KDD_GIDLp] ,[KDD_ObiTyp] ,[KDD_ObiFirma],[KDD_ObiNumer],[KDD_ObiLp]  ,[KDD_ObiDTDC] "
                                                 + "  ,[KDD_ObiDTWO] ,[KDD_Rodzaj]  ,[KDD_Magazyn] ,[KDD_Naliczono] ,[KDD_Kod] ,[KDD_Nazwa] ,[KDD_Data] ,[KDD_Wartosc]  ,[KDD_Waluta])  VALUES (@GidTyp ,@GidFirma "
                                                 + "  ,@GidNumer , @GidLp  , 0  ,@GidFirma ,0  , 0  , 0  , 0  , 1669  , 1   , 0   , '' , ''   ,@Data ,@Wartosc  , '')", connection);
     insertPalety.Parameters.Add("@GidNumer", SqlDbType.Int);
     insertPalety.Parameters["@GidNumer"].Value = dok.GIDNumer;
     insertPalety.Parameters.Add("@GidTyp", SqlDbType.Int);
     insertPalety.Parameters["@GidTyp"].Value = dok.GIDTyp;
     insertPalety.Parameters.Add("@GidFirma", SqlDbType.Int);
     insertPalety.Parameters["@GidFirma"].Value = dok.GIDFirma;
     insertPalety.Parameters.Add("@GidLp", SqlDbType.Int);
     insertPalety.Parameters["@GidLp"].Value = lp_koszt;
     insertPalety.Parameters.Add("@Data", SqlDbType.Int);
     insertPalety.Parameters["@Data"].Value = dok.Data;
     insertPalety.Parameters.Add("@Wartosc", SqlDbType.Decimal);
     insertPalety.Parameters["@Wartosc"].Value = paletyWartosc;

     insertPalety.ExecuteNonQuery();

     lp_palety = lp_koszt;
     lp_koszt++;

 }


 if (bonusZarzadu)
 {

     SqlCommand selectZarzadWartosc = new SqlCommand(" if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                     + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                     + " WHERE AtK_Nazwa = 'Bonus zarządu [%]' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo >= @data  and  ath_TimeStamp <= @data) "
                                                     + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                     + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                     + " WHERE AtK_Nazwa = 'Bonus zarządu [%]' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data else SELECt 0", connection);
     selectZarzadWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
     selectZarzadWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
     selectZarzadWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
     selectZarzadWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
     selectZarzadWartosc.Parameters.Add("@data", SqlDbType.Int);
     selectZarzadWartosc.Parameters["@data"].Value = data;
     zarzadWartosc = Convert.ToDecimal(selectZarzadWartosc.ExecuteScalar().ToString());


     if (zarzadWartosc == 0)
     {

         MessageBox.Show("Brak atrybutu 'Bonus zarządu [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
         zam.Wersja = XLWersja;
         zam.Tryb = -1;
         blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);

     }






     SqlCommand insertZarzad = new SqlCommand("INSERT INTO [CDN].[KosztyDodDok] ([KDD_GIDTyp] ,[KDD_GIDFirma] ,[KDD_GIDNumer] ,[KDD_GIDLp] ,[KDD_ObiTyp] ,[KDD_ObiFirma],[KDD_ObiNumer],[KDD_ObiLp]  ,[KDD_ObiDTDC] "
                                                 + "  ,[KDD_ObiDTWO] ,[KDD_Rodzaj]  ,[KDD_Magazyn] ,[KDD_Naliczono] ,[KDD_Kod] ,[KDD_Nazwa] ,[KDD_Data] ,[KDD_Wartosc]  ,[KDD_Waluta])  VALUES (@GidTyp ,@GidFirma "
                                                 + "  ,@GidNumer , @GidLp  , 0  ,@GidFirma ,0  , 0  , 0  , 0  , 965  , 1   , 0   , '' , ''   ,@Data ,@Wartosc  , '')", connection);
     insertZarzad.Parameters.Add("@GidNumer", SqlDbType.Int);
     insertZarzad.Parameters["@GidNumer"].Value = dok.GIDNumer;
     insertZarzad.Parameters.Add("@GidTyp", SqlDbType.Int);
     insertZarzad.Parameters["@GidTyp"].Value = dok.GIDTyp;
     insertZarzad.Parameters.Add("@GidFirma", SqlDbType.Int);
     insertZarzad.Parameters["@GidFirma"].Value = dok.GIDFirma;
     insertZarzad.Parameters.Add("@GidLp", SqlDbType.Int);
     insertZarzad.Parameters["@GidLp"].Value = lp_koszt;
     insertZarzad.Parameters.Add("@Data", SqlDbType.Int);
     insertZarzad.Parameters["@Data"].Value = dok.Data;
     insertZarzad.Parameters.Add("@Wartosc", SqlDbType.Decimal);
     insertZarzad.Parameters["@Wartosc"].Value = zarzadWartosc;

     insertZarzad.ExecuteNonQuery();

     lp_zarzad = lp_koszt;
     lp_koszt++;

 }



 if (zyskOddzialu)
 {

     /* SqlCommand selectZyskWartosc = new SqlCommand("SELECt SUM(Atr_Wartosc/100.0* TrE_Cena*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                     + " WHERE AtK_Nazwa = 'Zysk handlowy oddziału' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
      selectZyskWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
      selectZyskWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
      selectZyskWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
      selectZyskWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
      oddzialWartosc = Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());*/


        /*SqlCommand selectZyskWartosc = new SqlCommand(" if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                        + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                        + " WHERE AtK_Nazwa = 'Zysk handlowy oddziału [%]' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo >= @data  and  ath_TimeStamp <= @data) "
                                                        + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                        + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                        + " WHERE AtK_Nazwa = 'Zysk handlowy oddziału [%]' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data else SELECt 0", connection);
        selectZyskWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
        selectZyskWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
        selectZyskWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
        selectZyskWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
        selectZyskWartosc.Parameters.Add("@data", SqlDbType.Int);
        selectZyskWartosc.Parameters["@data"].Value = data;
        oddzialWartosc = Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());


        if (oddzialWartosc == 0)
        {

            MessageBox.Show("Brak atrybutu 'Zysk handlowy oddziału [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
            zam.Wersja = XLWersja;
            zam.Tryb = -1;
            blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);

        }



        SqlCommand insertOddzial = new SqlCommand("INSERT INTO [CDN].[KosztyDodDok] ([KDD_GIDTyp] ,[KDD_GIDFirma] ,[KDD_GIDNumer] ,[KDD_GIDLp] ,[KDD_ObiTyp] ,[KDD_ObiFirma],[KDD_ObiNumer],[KDD_ObiLp]  ,[KDD_ObiDTDC] "
                                                    + "  ,[KDD_ObiDTWO] ,[KDD_Rodzaj]  ,[KDD_Magazyn] ,[KDD_Naliczono] ,[KDD_Kod] ,[KDD_Nazwa] ,[KDD_Data] ,[KDD_Wartosc]  ,[KDD_Waluta])  VALUES (@GidTyp ,@GidFirma "
                                                    + "  ,@GidNumer , @GidLp  , 0  ,@GidFirma ,0  , 0  , 0  , 0  , 966  , 1   , 0   , '' , ''   ,@Data ,@Wartosc  , '')", connection);
        insertOddzial.Parameters.Add("@GidNumer", SqlDbType.Int);
        insertOddzial.Parameters["@GidNumer"].Value = dok.GIDNumer;
        insertOddzial.Parameters.Add("@GidTyp", SqlDbType.Int);
        insertOddzial.Parameters["@GidTyp"].Value = dok.GIDTyp;
        insertOddzial.Parameters.Add("@GidFirma", SqlDbType.Int);
        insertOddzial.Parameters["@GidFirma"].Value = dok.GIDFirma;
        insertOddzial.Parameters.Add("@GidLp", SqlDbType.Int);
        insertOddzial.Parameters["@GidLp"].Value = lp_koszt;
        insertOddzial.Parameters.Add("@Data", SqlDbType.Int);
        insertOddzial.Parameters["@Data"].Value = dok.Data;
        insertOddzial.Parameters.Add("@Wartosc", SqlDbType.Decimal);
        insertOddzial.Parameters["@Wartosc"].Value = oddzialWartosc;

        insertOddzial.ExecuteNonQuery();

        lp_oddzial = lp_koszt;
        lp_koszt++;

    }

    #endregion

    #region dodawanie kosztów Dod
    if (palety)
    {

        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
        koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.GIDLp = lp_palety;
        koszt.GIDNumer = dok.GIDNumer;
        koszt.GIDTyp = dok.GIDTyp;
        koszt.Wersja = XLWersja;
        koszt.Akcja = 0;
        koszt.Biezacy = Convert.ToString(Math.Round(paletyWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Inne = Convert.ToString(Math.Round(paletyWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Naliczono = Convert.ToString(Math.Round(paletyWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
       // MessageBox.Show("Koszt palet Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        if (blad != 0)
        {
            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla palet KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

    }

    if (transport)
    {

        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
        koszt.DoNaliczenia = Convert.ToString(Math.Round(transportWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.GIDLp = lp_transport;
        koszt.GIDNumer = dok.GIDNumer;
        koszt.GIDTyp = dok.GIDTyp;
        koszt.Wersja = XLWersja;
        koszt.Akcja = 0;
        koszt.Biezacy = Convert.ToString(Math.Round(transportWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Inne = Convert.ToString(Math.Round(transportWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Naliczono = Convert.ToString(Math.Round(transportWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
        //MessageBox.Show("Koszt transportu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        if (blad != 0)
        {
            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla transportu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

    }

    if (bonusZarzadu)
    {

        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
        koszt.DoNaliczenia = Convert.ToString(Math.Round(zarzadWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.GIDLp = lp_zarzad;
        koszt.GIDNumer = dok.GIDNumer;
        koszt.GIDTyp = dok.GIDTyp;
        koszt.Wersja = XLWersja;
        koszt.Akcja = 0;
        koszt.Biezacy = Convert.ToString(Math.Round(zarzadWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Inne = Convert.ToString(Math.Round(zarzadWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Naliczono = Convert.ToString(Math.Round(zarzadWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
        //MessageBox.Show("Koszt bonusu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        if (blad != 0)
        {
            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla bonusu zarządu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

    }

    if (zyskOddzialu)
    {

        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
        koszt.DoNaliczenia = Convert.ToString(Math.Round(oddzialWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.GIDLp = lp_oddzial;
        koszt.GIDNumer = dok.GIDNumer;
        koszt.GIDTyp = dok.GIDTyp;
        koszt.Wersja = XLWersja;
        koszt.Akcja = 0;
        koszt.Biezacy = Convert.ToString(Math.Round(oddzialWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Inne = Convert.ToString(Math.Round(oddzialWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        koszt.Naliczono = Convert.ToString(Math.Round(oddzialWartosc, 2, MidpointRounding.AwayFromZero) * 10);
        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
        //MessageBox.Show("Koszt zysku Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        if (blad != 0)
        {
            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla zysku handlowego oddziału KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

    }







    #endregion

    #region rozbicieKDZ

    XLRozbicieKDZInfo_20221 rozbij = new XLRozbicieKDZInfo_20221();
    rozbij.TrybRozbicia = 2;
    rozbij.Wersja = XLWersja;
    blad = cdn_api.cdn_api.XLRozbijKDZ(nrDok, rozbij);
    if (blad != 0)
    {
        MessageBox.Show("Błąd rozbicia dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        XLKomunikatInfo_20221 kom = new XLKomunikatInfo_20221();
        kom.Wersja = XLWersja;
        kom.Funkcja = 147;
        kom.Blad = blad;
        cdn_api.cdn_api.XLOpisBledu(kom);
        MessageBox.Show("Błąd rozbicia dokumentu KDZ: " + kom.OpisBledu, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }


    #endregion


    #region update TraNag

    decimal wartosc = oddzialWartosc + paletyWartosc + transportWartosc + zarzadWartosc;

    SqlCommand updateTraNag = new SqlCommand("update cdn.TraNag set TrN_WartoscDPPrzed=@Wartosc, TrN_WartoscDPPo=@Wartosc where TrN_GIDTyp=@GidTyp AND TrN_GIDFirma=@GidFirma AND TrN_GIDNumer =@GidNumer", connection);

    updateTraNag.Parameters.Add("@GidNumer", SqlDbType.Int);
    updateTraNag.Parameters["@GidNumer"].Value = dok.GIDNumer;
    updateTraNag.Parameters.Add("@GidTyp", SqlDbType.Int);
    updateTraNag.Parameters["@GidTyp"].Value = dok.GIDTyp;
    updateTraNag.Parameters.Add("@GidFirma", SqlDbType.Int);
    updateTraNag.Parameters["@GidFirma"].Value = dok.GIDFirma;
    updateTraNag.Parameters.Add("@Wartosc", SqlDbType.Decimal);
    updateTraNag.Parameters["@Wartosc"].Value = wartosc;

    updateTraNag.ExecuteNonQuery();



    #endregion



    XLKosztDodDokInfo_20221 koszt2 = new XLKosztDodDokInfo_20221();
    koszt2.Wersja = XLWersja;
    koszt2.Akcja = -1;
    blad = cdn_api.cdn_api.XLKosztyDodDok(koszt2);
    if (blad != 0)
    {
        MessageBox.Show("Błąd czyszczenia kosztów dodatkowych KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    XLZamkniecieDokumentuInfo_20221 zamDok = new XLZamkniecieDokumentuInfo_20221();
    zamDok.Wersja = XLWersja;
    zamDok.Tryb = trybZam;
    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zamDok);
    if (blad != 0)
    {
        MessageBox.Show("Błąd zamykania dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    if(paletyWartosc<0 || oddzialWartosc<0 || transportWartosc<0 || zarzadWartosc<0)
    {
        SqlCommand updateTraElem = new SqlCommand("update e set TrE_KosztKsiegowy= ROUND(Tre_Ilosc*Tre_Cena*(TrN_WartoscDPPrzed/(SELECT SUM(Tre_Ilosc*Tre_Cena) from cdn.TraElem where TrE_GIDTyp=@GidTyp AND TrE_GIDFirma=@GidFirma AND TrE_GIDNumer =@GidNumer  )),2), " +
            "TrE_KosztRzeczywisty=ROUND(Tre_Ilosc*Tre_Cena*(TrN_WartoscDPPrzed/(SELECT SUM(Tre_Ilosc*Tre_Cena) from cdn.TraElem where TrE_GIDTyp=@GidTyp AND TrE_GIDFirma=@GidFirma AND TrE_GIDNumer =@GidNumer  )),2) " +
            "from cdn.traElem as e join cdn.TraNag on TrN_GIDNumer=Tre_GIDNumer and TrN_GIDTyp= TrE_GIDTyp where TrE_GIDTyp=@GidTyp AND TrE_GIDFirma=@GidFirma AND TrE_GIDNumer =@GidNumer", connection);

        updateTraElem.Parameters.Add("@GidNumer", SqlDbType.Int);
        updateTraElem.Parameters["@GidNumer"].Value = dok.GIDNumer;
        updateTraElem.Parameters.Add("@GidTyp", SqlDbType.Int);
        updateTraElem.Parameters["@GidTyp"].Value = dok.GIDTyp;
        updateTraElem.Parameters.Add("@GidFirma", SqlDbType.Int);
        updateTraElem.Parameters["@GidFirma"].Value = dok.GIDFirma;

        updateTraElem.ExecuteNonQuery();


        SqlCommand updateTraSElem = new SqlCommand("update e set TrS_KosztKsiegowy= TrE_KosztKsiegowy, " +
            "TrS_KosztRzeczywisty=TrE_KosztRzeczywisty " +
            "from cdn.traSElem as e  " +
            "join cdn.TraElem on TrE_GIDNumer=TrS_GIDNumer and TrE_GIDTyp= TrS_GIDTyp and Tre_GIDLp=TrS_GIDLp and Trs_SubGidLp=1 where TrS_GIDTyp=@GidTyp AND TrS_GIDFirma=@GidFirma AND TrS_GIDNumer =@GidNumer", connection);

        updateTraSElem.Parameters.Add("@GidNumer", SqlDbType.Int);
        updateTraSElem.Parameters["@GidNumer"].Value = dok.GIDNumer;
        updateTraSElem.Parameters.Add("@GidTyp", SqlDbType.Int);
        updateTraSElem.Parameters["@GidTyp"].Value = dok.GIDTyp;
        updateTraSElem.Parameters.Add("@GidFirma", SqlDbType.Int);
        updateTraSElem.Parameters["@GidFirma"].Value = dok.GIDFirma;

        updateTraSElem.ExecuteNonQuery();
    }

    connection.Close();
}

}*/
        #endregion

        public static List<ListaDok> SelectListDoki(string query, string srcTableName, string connection_string)
        {
            List<ListaDok> str = new List<ListaDok>();
            string sttr = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, srcTableName);
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {

                        str.Add(new ListaDok()
                        {
                            GIDTyp = Convert.ToInt16(row[0].ToString()),
                            GIDFirma = Convert.ToInt32(row[1].ToString()),
                            GIDNumer = Convert.ToInt32(row[2].ToString()),
                            GIDLp = Convert.ToInt16(row[3].ToString()),
                        });



                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(sttr = sttr + "Bład SQL:" + ex.ToString() + "\n");
            }
            return str;
        }
        bool ZbieranieFZ(ADODB._Recordset recordset)
        {

            try
            {
                StringBuilder dokumenty = new StringBuilder();
                if (recordset != null)
                {
                    int count = recordset.RecordCount;

                    if (count != 0)
                    {
                        recordset.MoveFirst();

                        while (recordset.EOF == false)
                        {
                            ADODB.Fields fields = recordset.Fields;
                            if (Convert.ToInt16(fields[0].Value) == 1521)
                            {

                                doki.Add(new ListaDok()
                                {
                                    GIDTyp = Convert.ToInt16(fields[0].Value),
                                    GIDFirma = Convert.ToInt32(fields[1].Value),
                                    GIDNumer = Convert.ToInt32(fields[2].Value),
                                    GIDLp = Convert.ToInt16(fields[3].Value),
                                });
                            }
                            recordset.MoveNext();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie zaznaczono dokumentów. Operacja anulowana.", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return true;
                    }
                }
                else
                {
                    MessageBox.Show("Nie zaznaczono dokumentów. Operacja anulowana.", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania zaznaczonych dokumentów --> " + ex.Message, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }
        #region kom

        /*  public List<int> SelectListRowColumn(string query, string srcTableName, string connection_string)
          {
              List<int> str = new List<int>();
              string sttr = "";
              try
              {
                  using (SqlConnection connection = new SqlConnection(connection_string))
                  {
                      SqlDataAdapter adapter = new SqlDataAdapter();
                      adapter.SelectCommand = new SqlCommand(query, connection);
                      DataSet dataSet = new DataSet();
                      adapter.Fill(dataSet, srcTableName);
                      foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                      {
                          foreach (DataColumn column in (InternalDataCollectionBase)dataSet.Tables[0].Columns)
                          {
                              str.Add(Convert.ToInt32(row[column].ToString()));

                              //MessageBox.Show(column + ": " + row[column].ToString());
                          }


                      }
                  }

              }
              catch (Exception ex)
              {
                  MessageBox.Show(sttr = sttr + "Bład SQL:" + ex.ToString() + "\n");
              }
              return str;
          }*/

    }

    
/*
    [SubscribeProcedure(Procedures.TrN_FZSpinacz, "KDZ")]
    public class KDZSpinacz : Callback
    {
        private long gidNumer = 0;
        private long gidTyp = 0;
        private long gidFirma = 0;
        private short gidLp = 0;

        // private Dane dane;
        public override void Init()
        {
            AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow));
            AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(Resize));
            // AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));
        }



        private bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
        {

            // ClaWindow Parent = GetWindow();
            //Rectangle WndBnd = GetWindow().Bounds;
            //zakladkaTabWewnetrzne = GetWindow().AllChildren["?TabFS"];

            //Bufor= Parent.AllChildren["?lEkr_TransakcjaDoBufora"];
            //Akronim = Parent.AllChildren["?Knt:Akronim"];
            //AddSubscription(false, Bufor.Id, Events.Accepted, new TakeEventDelegate(OnCloseWindow));
            gidNumer = TraNag.TrN_GIDNumer;
            gidFirma = TraNag.TrN_GIDFirma;
            gidLp = TraNag.TrN_GIDLp;
            gidTyp = TraNag.TrN_GIDTyp;

            AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));

            //MessageBox.Show("GID Numer: " + gidNumer, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //listID = GetWindow().AllChildren["?Handlowe"].Id;      //tu sprawdzic

            //pobrane z pola w zakładce Raporty
            return true;
        }

        public bool Resize(Procedures ProcId, int ControlId, Events Event)
        {

            return true;
        }

        public bool OnCloseWindow(Procedures ProcId, int ControlId, Events Event)
        {

            try
            {
                string connection_string = "";
                //MessageBox.Show("GID numer: " + gidNumer, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                int XLWersja = 20221;
                XLLoginInfo_20221 xlLoginInfo20221 = new XLLoginInfo_20221();
                xlLoginInfo20221.Wersja = XLWersja;
                xlLoginInfo20221.UtworzWlasnaSesje = 0;
                xlLoginInfo20221.Winieta = -1;
                xlLoginInfo20221.TrybWsadowy = 1;
                xlLoginInfo20221.ProgramID = "KZD";
                int sesja = 0;
                int num3 = cdn_api.cdn_api.XLLogin(xlLoginInfo20221, ref sesja);
                if (num3 != 0)
                {
                    MessageBox.Show("Błąd logowania! (API " + num3.ToString() + ")");
                }
                else
                {
                    //MessageBox.Show("-------- Rozpoczęcie działania programu --------");
                    //Console.WriteLine();
                    XLPolaczenieInfo_20221 polaczenieInfo20221 = new XLPolaczenieInfo_20221();
                    polaczenieInfo20221.Wersja = XLWersja;
                    if (cdn_api.cdn_api.XLPolaczenie(polaczenieInfo20221) != 0)
                    {
                        MessageBox.Show("Błąd połączenia do bazy SQL!");
                    }
                    SqlConnection connection = new SqlConnection();
                    try
                    {
                        // connection = new SqlConnection(polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")));
                        connection = new SqlConnection((polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", ""));
                        connection_string = (polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", "");

                        //MessageBox.Show(polaczenieInfo20220.ConnectString.Substring(polaczenieInfo20220.ConnectString.IndexOf("Data Source"), polaczenieInfo20220.ConnectString.Length - polaczenieInfo20220.ConnectString.IndexOf("Data Source")) + "; User ID = Wydruk; Password=S@msun6B@");
                    }
                    catch
                    {
                        MessageBox.Show("Błąd generowania połączenia " + polaczenieInfo20221.ConnectString);
                    }

                    connection.Open();
                    SqlCommand sqlCommand1 = new SqlCommand("select TrN_Stan from cdn.TraNag where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                    sqlCommand1.Parameters.Add("@GidNumer", SqlDbType.Int);
                    sqlCommand1.Parameters["@GidNumer"].Value = gidNumer;
                    sqlCommand1.Parameters.Add("@GidTyp", SqlDbType.Int);
                    sqlCommand1.Parameters["@GidTyp"].Value = gidTyp;
                    string stan = sqlCommand1.ExecuteScalar().ToString().Trim();

                    SqlCommand czyIstniejeQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE(AtK_Nazwa = 'Bonus zarządu [%]' or Atk_Nazwa = 'Zysk handlowy oddziału [%]' or Atk_Nazwa = 'Transport' or Atk_Nazwa = 'Palety') and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                "ELSE SELECT 'NIE'", connection);
                    czyIstniejeQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    czyIstniejeQuery.Parameters["@GidNumer"].Value = gidNumer;
                    czyIstniejeQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    czyIstniejeQuery.Parameters["@GidTyp"].Value = gidTyp;
                    string czyIstnieje = czyIstniejeQuery.ExecuteScalar().ToString();


                    SqlCommand iloscQuery = new SqlCommand("SELECT (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp and Twr_Typ != 4) - " +
                                                                " (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE AtK_Nazwa = 'Palety'  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp)", connection);
                    iloscQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    iloscQuery.Parameters["@GidNumer"].Value = gidNumer;
                    iloscQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    iloscQuery.Parameters["@GidTyp"].Value = gidTyp;
                    int ilosc = Convert.ToInt32(iloscQuery.ExecuteScalar().ToString());

                    //MessageBox.Show("Stan: " + stan);

                    if (czyIstnieje == "TAK" && stan == "3" && ilosc > 0)// || (stan=="2" && Bufor.CheckedRaw=="0")))
                    {

                        //MessageBox.Show("Istnieje atrybut", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        SqlCommand akronimQuery = new SqlCommand("SELECT  knt_Akronim from cdn.TraNAg join cdn.KntKarty on TrN_KntNumer = Knt_GIDNumer and TrN_KntTyp = Knt_GIDTyp where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        akronimQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        akronimQuery.Parameters["@GidNumer"].Value = gidNumer;
                        akronimQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        akronimQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string akronim = akronimQuery.ExecuteScalar().ToString();


                        SqlCommand seriaQuery = new SqlCommand("SELECT TrN_TrNSeria from cdn.TraNAg where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        seriaQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        seriaQuery.Parameters["@GidNumer"].Value = gidNumer;
                        seriaQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        seriaQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string seria = seriaQuery.ExecuteScalar().ToString();


                        SqlCommand numerDokQuery = new SqlCommand("SELECT cdn.NumerDokumentu(TrN_GIDTyp, TrN_SpiTyp, TrN_TrNTyp, TrN_TrNNumer, TrN_TrNRok, TrN_TrNSeria, TrN_TrNMiesiac) from cdn.TraNAg  where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                        numerDokQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        numerDokQuery.Parameters["@GidNumer"].Value = gidNumer;
                        numerDokQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        numerDokQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string numerDok = numerDokQuery.ExecuteScalar().ToString();

                        SqlCommand czyBonusZarzaduQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE AtK_Nazwa = 'Bonus zarządu [%]'  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                                "ELSE SELECT 'NIE'", connection);
                        czyBonusZarzaduQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyBonusZarzaduQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyBonusZarzaduQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyBonusZarzaduQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyBonusZarzadu = czyBonusZarzaduQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        SqlCommand czyZyskOddzialuQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE AtK_Nazwa = 'Zysk handlowy oddziału [%]'  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyZyskOddzialuQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyZyskOddzialuQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyZyskOddzialuQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyZyskOddzialuQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyZyskOddzialu = czyZyskOddzialuQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        SqlCommand czyTransportQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE AtK_Nazwa = 'Transport'  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyTransportQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyTransportQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyTransportQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyTransportQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyTransport = czyTransportQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        SqlCommand czyPaletyQuery = new SqlCommand("if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                               " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                               " WHERE AtK_Nazwa = 'Palety'  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp) SELECT 'TAK' " +
                                                               "ELSE SELECT 'NIE'", connection);
                        czyPaletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        czyPaletyQuery.Parameters["@GidNumer"].Value = gidNumer;
                        czyPaletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        czyPaletyQuery.Parameters["@GidTyp"].Value = gidTyp;
                        bool czyPalety = czyPaletyQuery.ExecuteScalar().ToString().Trim() == "TAK";

                        SqlCommand iloscBonusZarzaduQuery = new SqlCommand("SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                 " WHERE AtK_Nazwa = 'Bonus zarządu [%]'  and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                        iloscBonusZarzaduQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        iloscBonusZarzaduQuery.Parameters["@GidNumer"].Value = gidNumer;
                        iloscBonusZarzaduQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        iloscBonusZarzaduQuery.Parameters["@GidTyp"].Value = gidTyp;
                        int iloscBonusZarzadu = Convert.ToInt32(iloscBonusZarzaduQuery.ExecuteScalar().ToString());


                        SqlCommand iloscZyskOddzialuQuery = new SqlCommand("SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                                " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                 " WHERE AtK_Nazwa = 'Zysk handlowy oddziału [%]'   and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                        iloscZyskOddzialuQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        iloscZyskOddzialuQuery.Parameters["@GidNumer"].Value = gidNumer;
                        iloscZyskOddzialuQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        iloscZyskOddzialuQuery.Parameters["@GidTyp"].Value = gidTyp;
                        int iloscZyskOddzialu = Convert.ToInt32(iloscZyskOddzialuQuery.ExecuteScalar().ToString());







                        /*SqlCommand typQuery = new SqlCommand("SELECT TOP (1) AtK_Nazwa from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE(AtK_Nazwa = 'Bonus zarządu' or Atk_Nazwa = 'Zysk handlowy oddziału' or Atk_Nazwa = 'Transport' or Atk_Nazwa = 'Palety') and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                        typQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        typQuery.Parameters["@GidNumer"].Value = gidNumer;
                        typQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        typQuery.Parameters["@GidTyp"].Value = gidTyp;
                        string typ = typQuery.ExecuteScalar().ToString();*/

    /*

                        XLZwiazanyDokInfo_20221 zwiazany = new XLZwiazanyDokInfo_20221();
                        zwiazany.Wersja = XLWersja;
                        zwiazany.GIDTyp = (int)gidTyp;
                        zwiazany.GIDNumer = (int)gidNumer;
                        int blad = cdn_api.cdn_api.XLDokumentyZwiazane(zwiazany);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania dokumentu związanego: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        int l = 0;


                        if (czyTransport == true || czyPalety == true)
                        {
                            if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                            {
                                if (iloscBonusZarzadu == iloscZyskOddzialu)
                                {
                                    if (iloscBonusZarzadu == ilosc)
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    }
                                }
                                else
                                {
                                    if (iloscBonusZarzadu == ilosc)
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    }
                                    else if (iloscZyskOddzialu == ilosc)
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    }
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    }
                                }
                            }
                            else
                            {
                                if (czyBonusZarzadu == true)
                                {
                                    if (iloscBonusZarzadu == ilosc)
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    }
                                }
                                else if (czyZyskOddzialu == true)
                                {
                                    if (iloscZyskOddzialu == ilosc)
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    else
                                    {
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                        DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                                    }

                                }
                                else
                                    DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                            }

                        }
                        else if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                        {
                            if (iloscBonusZarzadu == iloscZyskOddzialu)
                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                            else
                            {
                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria);
                                DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                            }
                        }
                        else
                        {
                            DodDok.dodajDokument(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, connection_string, 1, (int)gidNumer, (int)gidTyp, ref l, seria);
                        }






                        XLZwiazanyDokInfo_20221 rozwiazany = new XLZwiazanyDokInfo_20221();
                        rozwiazany.Wersja = XLWersja;
                        rozwiazany.Akcja = -1;
                        blad = cdn_api.cdn_api.XLDokumentyZwiazane(rozwiazany);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd usuwania dokumentu związanego: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }



                        blad = cdn_api.cdn_api.XLLogout(sesja);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd wylogowywania: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                    }

                    connection.Close();
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Błąd funkcji onCloseWindow: " + e.Message, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

        }


        public string ConnectToSQL(string connection_string, string query)
        {
            string str = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, "Query");
                    if (dataSet.Tables.Count == 0)
                    {
                        return "";
                    }
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {
                        str = row[0].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                str = str + "Bład SQL:" + ex.ToString() + "\n";
            }

            return str;
        }

        
        public override void Cleanup()
        {
        }

    
    */
    #endregion


    
    [SubscribeProcedure(Procedures.TrN_FZSpinacz, "KDZ")]
    public class KDZSpinacz : Callback
    {
        private long gidNumer = 0;
        private long gidTyp = 0;
        private long gidFirma = 0;
        private short gidLp = 0;
        private int SpiTyp = 0;

        // private Dane dane;
        public override void Init()
        {
            AddSubscription(true, 0, Events.OpenWindow, new TakeEventDelegate(OnOpenWindow));
            AddSubscription(false, 0, Events.ResizeWindow, new TakeEventDelegate(Resize));
            // AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));
        }



        private bool OnOpenWindow(Procedures ProcId, int ControlId, Events Event)
        {

            // ClaWindow Parent = GetWindow();
            //Rectangle WndBnd = GetWindow().Bounds;
            //zakladkaTabWewnetrzne = GetWindow().AllChildren["?TabFS"];

            //Bufor= Parent.AllChildren["?lEkr_TransakcjaDoBufora"];
            //Akronim = Parent.AllChildren["?Knt:Akronim"];
            //AddSubscription(false, Bufor.Id, Events.Accepted, new TakeEventDelegate(OnCloseWindow));
            gidNumer = TraNag.TrN_GIDNumer;
            gidFirma = TraNag.TrN_GIDFirma;
            gidLp = TraNag.TrN_GIDLp;
            gidTyp = TraNag.TrN_GIDTyp;
            SpiTyp = TraNag.TrN_SpiTyp;


            AddSubscription(false, 0, Events.Completed, new TakeEventDelegate(OnCloseWindow));

           // MessageBox.Show("SpiTyp: " +SpiTyp, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //listID = GetWindow().AllChildren["?Handlowe"].Id;      //tu sprawdzic

            //pobrane z pola w zakładce Raporty
            return true;
        }

        public bool Resize(Procedures ProcId, int ControlId, Events Event)
        {

            return true;
        }

        public bool OnCloseWindow(Procedures ProcId, int ControlId, Events Event)
        {

            try
            {
                string connection_string = "";
                //MessageBox.Show("GID numer: " + gidNumer, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                int XLWersja = 20221;
                XLLoginInfo_20221 xlLoginInfo20221 = new XLLoginInfo_20221();
                xlLoginInfo20221.Wersja = XLWersja;
                xlLoginInfo20221.UtworzWlasnaSesje = 0;
                xlLoginInfo20221.Winieta = -1;
                xlLoginInfo20221.TrybWsadowy = 1;
                xlLoginInfo20221.ProgramID = "KZD";
                int sesja = 0;
                int num3 = cdn_api.cdn_api.XLLogin(xlLoginInfo20221, ref sesja);
                if (num3 != 0)
                {
                    MessageBox.Show("Błąd logowania! (API " + num3.ToString() + ")");
                }
                else
                {
                    //MessageBox.Show("-------- Rozpoczęcie działania programu --------");
                    //Console.WriteLine();
                    XLPolaczenieInfo_20221 polaczenieInfo20221 = new XLPolaczenieInfo_20221();
                    polaczenieInfo20221.Wersja = XLWersja;
                    if (cdn_api.cdn_api.XLPolaczenie(polaczenieInfo20221) != 0)
                    {
                        MessageBox.Show("Błąd połączenia do bazy SQL!");
                    }
                    SqlConnection connection = new SqlConnection();
                    try
                    {
                        // connection = new SqlConnection(polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")));
                        connection = new SqlConnection((polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", ""));
                        connection_string = (polaczenieInfo20221.ConnectString.Substring(polaczenieInfo20221.ConnectString.IndexOf("Data Source"), polaczenieInfo20221.ConnectString.Length - polaczenieInfo20221.ConnectString.IndexOf("Data Source")) + "User ID = sabisup; Password=#0ut2510gro0ne.34").Replace("Integrated Security=SSPI", "");

                        //MessageBox.Show(polaczenieInfo20220.ConnectString.Substring(polaczenieInfo20220.ConnectString.IndexOf("Data Source"), polaczenieInfo20220.ConnectString.Length - polaczenieInfo20220.ConnectString.IndexOf("Data Source")) + "; User ID = Wydruk; Password=S@msun6B@");
                    }
                    catch
                    {
                        MessageBox.Show("Błąd generowania połączenia " + polaczenieInfo20221.ConnectString);
                    }

                    List<ListaDok> doki = new List<ListaDok>();
                    string gidNumery = "";

                    if (SpiTyp == 0)
                    {
                        string PZtyQuery = "SELECT TrN_GIDTyp, TrN_GIDFirma ,TrN_GIDNumer,TrN_GIDLp  FROM cdn.TraNag WHERE TrN_GIDTyp = 1489 and TrN_SpiTyp = 1521 and TrN_SpiNumer = " + gidNumer;
                        doki = SelectListDoki(PZtyQuery, "cdn.TraNag", connection_string);

                        for (int i = 0; i < doki.Count; i++)
                        {
                            gidNumery += doki[i].GIDNumer.ToString();
                            if (i != (doki.Count - 1))
                            {
                                gidNumery += ",";
                            }
                        }
                    }
                    else
                    {
                        string PZtyQuery = "SELECT TrS_SpiTyp, TrS_SpiFirma ,TrS_SpiNumer,TrS_SpiLp  FROM cdn.TraSElem WHERE TrS_GIDTyp = 1521 and TrS_SpiTyp = 1489 and TrS_GIDNumer =" + gidNumer
                                            + " group by TrS_SpiLp, TrS_SpiNumer, TrS_SpiFirma, TrS_SpiTyp";
                        doki = SelectListDoki(PZtyQuery, "cdn.TraSElem", connection_string);

                        gidNumery += "(";
                        
                        for (int i = 0; i < doki.Count; i++)
                        {
                            gidNumery += " (TrE_GIDNumer =" + doki[i].GIDNumer.ToString() + " and TrE_GIDLp=" + doki[i].GIDLp.ToString() + ") ";
                            
                            if (i != (doki.Count - 1))
                            {
                                gidNumery += " or ";
                            }
                        }

                        gidNumery += ")";
                    }

                    //MessageBox.Show("GId numery: " + gidNumery);
                    connection.Open();
                    SqlCommand sqlCommand1 = new SqlCommand("select TrN_Stan from cdn.TraNag where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                    sqlCommand1.Parameters.Add("@GidNumer", SqlDbType.Int);
                    sqlCommand1.Parameters["@GidNumer"].Value = gidNumer;
                    sqlCommand1.Parameters.Add("@GidTyp", SqlDbType.Int);
                    sqlCommand1.Parameters["@GidTyp"].Value = gidTyp;
                    string stan = sqlCommand1.ExecuteScalar().ToString().Trim();

                    SqlCommand frsQuery = new SqlCommand("SELECT TrN_FrsID from cdn.TraNAg where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                    frsQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    frsQuery.Parameters["@GidNumer"].Value = gidNumer;
                    frsQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    frsQuery.Parameters["@GidTyp"].Value = gidTyp;
                    string frs = frsQuery.ExecuteScalar().ToString();


                    SqlCommand numerDokQuery = new SqlCommand("SELECT cdn.NumerDokumentu(TrN_GIDTyp, TrN_SpiTyp, TrN_TrNTyp, TrN_TrNNumer, TrN_TrNRok, TrN_TrNSeria, TrN_TrNMiesiac) from cdn.TraNAg  where TrN_GIDNumer=@GidNumer and TrN_GIDTyp=@GidTyp", connection);
                    numerDokQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    numerDokQuery.Parameters["@GidNumer"].Value = gidNumer;
                    numerDokQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    numerDokQuery.Parameters["@GidTyp"].Value = gidTyp;
                    string numerDok = numerDokQuery.ExecuteScalar().ToString();

                    if (SpiTyp == 0)
                    {
                        int iloscDok = 0;
                        DodDok.WykonajKDZ(connection_string, gidNumery, stan, (int)gidNumer,(int) gidTyp, doki, connection, XLWersja, sesja, numerDok, ref iloscDok, frs);
                        
                    }
                    else
                    {
                        int iloscDok = 0;
                        DodDok.WykonajKDZMalySpinacz(connection_string, gidNumery, stan, (int)gidNumer, (int)gidTyp, doki, connection, XLWersja, sesja, numerDok, ref iloscDok, frs);
                    }

                    XLZwiazanyDokInfo_20221 rozwiazany = new XLZwiazanyDokInfo_20221();
                    rozwiazany.Wersja = XLWersja;
                    rozwiazany.Akcja = -1;
                    int blad = cdn_api.cdn_api.XLDokumentyZwiazane(rozwiazany);
                    if (blad != 0)
                    {
                        MessageBox.Show("Błąd usuwania dokumentu związanego: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }



                   

                    connection.Close();
                                    }
                int blad2 = cdn_api.cdn_api.XLLogout(sesja);
                if (blad2 != 0)
                {
                    MessageBox.Show("Błąd wylogowywania: " + blad2, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return true;
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show("Błąd funkcji onCloseWindow: " + e.Message, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return false;
                                }

                            }


                            public string ConnectToSQL(string connection_string, string query)
                            {
                                string str = "";
                                try
                                {
                                    using (SqlConnection connection = new SqlConnection(connection_string))
                                    {
                                        SqlDataAdapter adapter = new SqlDataAdapter();
                                        adapter.SelectCommand = new SqlCommand(query, connection);
                                        DataSet dataSet = new DataSet();
                                        adapter.Fill(dataSet, "Query");
                                        if (dataSet.Tables.Count == 0)
                                        {
                                            return "";
                                        }
                                        foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                                        {
                                            str = row[0].ToString();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    str = str + "Bład SQL:" + ex.ToString() + "\n";
                                }

                                return str;
                            }


        public static List<ListaDok> SelectListDoki(string query, string srcTableName, string connection_string)
        {
            List<ListaDok> str = new List<ListaDok>();
            string sttr = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, srcTableName);
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {

                        str.Add(new ListaDok()
                        {
                            GIDTyp = Convert.ToInt16(row[0].ToString()),
                            GIDFirma = Convert.ToInt32(row[1].ToString()),
                            GIDNumer = Convert.ToInt32(row[2].ToString()),
                            GIDLp = Convert.ToInt16(row[3].ToString()),
                        });



                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(sttr = sttr + "Bład SQL:" + ex.ToString() + "\n");
            }
            return str;
        }


        public override void Cleanup()
                            {
                            }

                        }
                    }





