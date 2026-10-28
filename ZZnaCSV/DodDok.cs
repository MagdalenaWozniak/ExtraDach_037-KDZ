using ADODB;
using cdn_api;
using Hydra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static KZD.Doki;

namespace KZD
{
    class DodDok
    {

        public static void dodajDokument(int sesja, int XLWersja, string akronim, string numerDok, bool bonusZarzadu, bool zyskOddzialu, bool transport, bool palety, bool paletyZl,  string connection_string, int trybZam, int gidNumer, int gidTyp, ref int iloscDok, string seria, string frs)

        {
            int nrDok = 0;
            string typ = (bonusZarzadu == true ? " Bonus zarządu " : "") + (zyskOddzialu == true ? " Zysk handlowy oddziału " : "") + (transport == true ? " Transport " : "") + (palety == true ? " Palety " : "")+(paletyZl == true ? " Palety [Zł] " : "");



            SqlConnection connection = new SqlConnection(connection_string);
            connection.Open();

            string czyIstnieje = "if exists(SELECT * FROM [CDN].[TrNOpisy] WHERE [TnO_Opis] like '" + akronim + " " + numerDok + " - " + typ + "%') SELECT 'TAK' else SELECT 'NIE'";
            SqlCommand czyIstniejeDok = new SqlCommand(czyIstnieje, connection);
           /* czyIstniejeDok.Parameters.Add("@opis", SqlDbType.VarChar);
            czyIstniejeDok.Parameters["@opis"].Value = akronim + " " + numerDok + " - " + typ+" AUTOMAT";*/
            string istniejeDok = czyIstniejeDok.ExecuteScalar().ToString();


            if (istniejeDok.Trim() == "NIE")
            {

                XLDokumentNagInfo_20221 dok = new XLDokumentNagInfo_20221();
                dok.Wersja = XLWersja;
                dok.Typ = 1232;
                dok.Zwiazane = 1;
                dok.PrzyczynaKorekty = "HAN";
                dok.Seria = seria != "" ? seria : "<PUS>";
                dok.Tryb = 2;
                dok.Opis = akronim + " " + numerDok + " - " + typ + " AUTOMAT";
                dok.FRSID = Convert.ToInt32(frs);
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
                                                                   " WHERE (Atk_id=265 or Atk_id=281) and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
                    List<int> pozycje = SelectListRowColumn(pozycjeQuery, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycje)
                    {
                        //MessageBox.Show("Usuwanie pozycji bonus zarządu: " +lp, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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



                SqlCommand paletyQuery = new SqlCommand("if exists( SELECT Tre_GIDLp FROM cdn.TraElem  WHERE TrE_TwrNazwa LIKE '%Palet%' and TrE_GIDTyp=@GidTyp AND TrE_GIDNumer=@GidNumer ) SELECT 'TAK' else SELECT 'NIE'", connection);
                paletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                paletyQuery.Parameters["@GidNumer"].Value = dok.GIDNumer;
                paletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                paletyQuery.Parameters["@GidTyp"].Value = dok.GIDTyp;
                string czyPaleta = paletyQuery.ExecuteScalar().ToString();

                // MessageBox.Show("Czy paleta: " + czyPaleta);
                if (czyPaleta.Trim() == "TAK")
                {
                    string pozycjeQueryPalety = "SELECt Tre_GIDLp FROM cdn.TraElem  WHERE TrE_TwrNazwa LIKE '%Palet%' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer; //"SELECt Tre_GIDLp FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                                                                                                                                           // " WHERE AtK_Nazwa = 'Palety' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer;
                    List<int> pozycjePalety = SelectListRowColumn(pozycjeQueryPalety, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycjePalety)
                    {
                        //MessageBox.Show("Pozycja palety: " + lp);
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
                                                                   " WHERE Atk_id=268 and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
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

                List<int> lp_transport = new List<int>();
                List<int> lp_palety = new List<int>();
                int lp_zarzad = 0;
                int lp_oddzial = 0;
                int lp_paletyZl = 0;

                int data = 0;

                decimal paletyWartosc = 0;
                List<decimal> paletyWartoscLp = new List<decimal>();
                string lpyPalety = "";
                List<string> lpyPaletySplit = new List<string>();

                decimal transportWartosc = 0;
                List<decimal> transportWartoscLp = new List<decimal>();
                string lpyTransport = "";
                List<string> lpyTransportSplit = new List<string>();

                decimal zarzadWartosc = 0;
                decimal oddzialWartosc = 0;
                decimal paletyZlWartosc = 0;
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

                    if (data == 0 && zyskOddzialu)
                    {
                        /*SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                        data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                        data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                        data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());*/
                        data = dok.Data;
                    }

                }

                try
                {
                    
                    SqlCommand LpTransportQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                       + " WHERE Atk_id=267 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                    LpTransportQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    LpTransportQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                    LpTransportQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    LpTransportQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                    object LpTransportObject = LpTransportQuery.ExecuteScalar();

                    if (LpTransportObject != null && LpTransportObject.ToString()!="")
                    {
                        lpyTransport = LpTransportObject.ToString();
                        lpyTransportSplit = lpyTransport.Split(',').ToList();
                    }



                    if (transport)
                    {
                        foreach (string lp in lpyTransportSplit)
                        {
                            SqlCommand selectTransportWartosc = new SqlCommand("SELECt ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                           + " WHERE Atk_id=267 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and TrE_GIDLp=@GidLp", connection);
                            selectTransportWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                            selectTransportWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                            selectTransportWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                            selectTransportWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                            selectTransportWartosc.Parameters.Add("@GidLp", SqlDbType.Int);
                            selectTransportWartosc.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                            transportWartoscLp.Add(Convert.ToDecimal(selectTransportWartosc.ExecuteScalar().ToString()));

                            transportWartosc += transportWartoscLp.Last();



                            SqlCommand selectLp = new SqlCommand("SELECT KDD_GIDLp from cdn.KosztyDodDok where  KDD_GIDNumer=@GidNumer  and KDD_GIDTyp =@GidTyp and KDD_ObiLp=@GidLp", connection);
                            selectLp.Parameters.Add("@GidNumer", SqlDbType.Int);
                            selectLp.Parameters["@GidNumer"].Value = dok.GIDNumer;
                            selectLp.Parameters.Add("@GidTyp", SqlDbType.Int);
                            selectLp.Parameters["@GidTyp"].Value = dok.GIDTyp;
                            selectLp.Parameters.Add("@GidLp", SqlDbType.Int);
                            selectLp.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                            lp_transport.Add(Convert.ToInt32(selectLp.ExecuteScalar().ToString()));



                            //lp_transport.Add(lp_koszt);
                            lp_koszt++;

                            string commandUpdate = "update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1, KDD_Wartosc="+transportWartoscLp.Last().ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp =" + lp;
                            SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
                            updateCommand.ExecuteNonQuery();
                        }
                       
                        // Update("update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1 where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")", connection_string);


                    }
                    else
                    {
                        //MessageBox.Show("Usuwamy pozycje ObiLp=" + lpyTransport + "\n" + "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")");
                        if (LpTransportObject != null && LpTransportObject.ToString() != "")
                        {
                            string command = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")";
                            SqlCommand deleteTransport = new SqlCommand(command, connection);
                            deleteTransport.ExecuteNonQuery();
                            //DeleteFrom("delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")", connection_string);
                            
                        }

                        string commandDelete2 = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=939";
                        //MessageBox.Show(commandDelete);
                        SqlCommand delete2 = new SqlCommand(commandDelete2, connection);
                        delete2.ExecuteNonQuery();

                    }
                }
                catch (Exception e)
                {
                    iloscDok--;
                    MessageBox.Show("Błąd atrybutu 'Transport'\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }

                try
                {
                    SqlCommand LpPaletyQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                       + " WHERE Atk_id=266 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                    LpPaletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    LpPaletyQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                    LpPaletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    LpPaletyQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                    object LpPaletyObject = LpPaletyQuery.ExecuteScalar();

                    if (LpPaletyObject != null && LpPaletyObject.ToString()!="")
                    {
                        lpyPalety = LpPaletyObject.ToString();
                        lpyPaletySplit = lpyPalety.Split(',').ToList();
                    }


                    if (palety)
                    {
                        foreach (string lp in lpyPaletySplit)
                        {
                            SqlCommand selectPaletyWartosc = new SqlCommand("SELECt ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                           + " WHERE Atk_id=266 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and TrE_GIDLp=@GidLp", connection);
                            selectPaletyWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                            selectPaletyWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                            selectPaletyWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                            selectPaletyWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                            selectPaletyWartosc.Parameters.Add("@GidLp", SqlDbType.Int);
                            selectPaletyWartosc.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                            paletyWartoscLp.Add(Convert.ToDecimal(selectPaletyWartosc.ExecuteScalar().ToString()));

                            paletyWartosc += paletyWartoscLp.Last();

                            SqlCommand selectLp = new SqlCommand("SELECT KDD_GIDLp from cdn.KosztyDodDok where  KDD_GIDNumer=@GidNumer  and KDD_GIDTyp =@GidTyp and KDD_ObiLp=@GidLp", connection);
                            selectLp.Parameters.Add("@GidNumer", SqlDbType.Int);
                            selectLp.Parameters["@GidNumer"].Value = dok.GIDNumer;
                            selectLp.Parameters.Add("@GidTyp", SqlDbType.Int);
                            selectLp.Parameters["@GidTyp"].Value = dok.GIDTyp;
                            selectLp.Parameters.Add("@GidLp", SqlDbType.Int);
                            selectLp.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                            lp_palety.Add(Convert.ToInt32(selectLp.ExecuteScalar().ToString()));



                            //lp_transport.Add(lp_koszt);
                            lp_koszt++;

                            //lp_palety.Add(lp_koszt);
                            //lp_koszt++;

                            string commandUpdate = "update cdn.KosztyDodDok set KDD_Rodzaj=1669, KDD_Magazyn=1, KDD_Wartosc="+paletyWartoscLp.Last().ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp=" + lp;
                            SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
                            updateCommand.ExecuteNonQuery();
                        }
                      
                        //Update("update cdn.KosztyDodDok set KDD_Rodzaj=1669, KDD_Magazyn=1 where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")", connection_string);


                    }
                    else
                    {
                       // MessageBox.Show("Usuwany palety\ndelete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")");
                        if (LpPaletyObject != null && LpPaletyObject.ToString() != "")
                        {
                            string command = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")";
                            SqlCommand deletePalety = new SqlCommand(command, connection);
                            deletePalety.ExecuteNonQuery();
                        }

                        string commandDelete2 = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=1669";
                        //MessageBox.Show(commandDelete);
                        SqlCommand delete2 = new SqlCommand(commandDelete2, connection);
                        delete2.ExecuteNonQuery();
                        // DeleteFrom("delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")", connection_string);

                    }
                }
                catch (Exception e)
                {
                    iloscDok--;
                    MessageBox.Show("Błąd atrybutu 'Palety'\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }

                string commandDelete = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=0";
                SqlCommand delete = new SqlCommand(commandDelete, connection);
                delete.ExecuteNonQuery();

                /*SqlCommand selectPaletyWartosc = new SqlCommand("SELECt SUM(ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                               + " WHERE Atk_id=266 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
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
                lp_koszt++;*/


                if (paletyZl)
                {
                    try
                    {
                        SqlCommand selectPaletyZlWartosc = new SqlCommand(" SELECt SUM(Atr_Wartosc*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                               + " WHERE Atk_id=280 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                        selectPaletyZlWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                        selectPaletyZlWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                        selectPaletyZlWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                        selectPaletyZlWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                        selectPaletyZlWartosc.Parameters.Add("@data", SqlDbType.Int);
                        selectPaletyZlWartosc.Parameters["@data"].Value = data;
                        paletyZlWartosc = Convert.ToDecimal(selectPaletyZlWartosc.ExecuteScalar().ToString());





                        SqlCommand insertPaletyZl = new SqlCommand("INSERT INTO [CDN].[KosztyDodDok] ([KDD_GIDTyp] ,[KDD_GIDFirma] ,[KDD_GIDNumer] ,[KDD_GIDLp] ,[KDD_ObiTyp] ,[KDD_ObiFirma],[KDD_ObiNumer],[KDD_ObiLp]  ,[KDD_ObiDTDC] "
                                                                    + "  ,[KDD_ObiDTWO] ,[KDD_Rodzaj]  ,[KDD_Magazyn] ,[KDD_Naliczono] ,[KDD_Kod] ,[KDD_Nazwa] ,[KDD_Data] ,[KDD_Wartosc]  ,[KDD_Waluta])  VALUES (@GidTyp ,@GidFirma "
                                                                    + "  ,@GidNumer , @GidLp  , 0  ,@GidFirma ,0  , 0  , 0  , 0  , 1669  , 1   , 0   , '' , ''   ,@Data ,@Wartosc  , '')", connection);
                        insertPaletyZl.Parameters.Add("@GidNumer", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidNumer"].Value = dok.GIDNumer;
                        insertPaletyZl.Parameters.Add("@GidTyp", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidTyp"].Value = dok.GIDTyp;
                        insertPaletyZl.Parameters.Add("@GidFirma", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidFirma"].Value = dok.GIDFirma;
                        insertPaletyZl.Parameters.Add("@GidLp", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidLp"].Value = lp_koszt;
                        insertPaletyZl.Parameters.Add("@Data", SqlDbType.Int);
                        insertPaletyZl.Parameters["@Data"].Value = dok.Data;
                        insertPaletyZl.Parameters.Add("@Wartosc", SqlDbType.Decimal);
                        insertPaletyZl.Parameters["@Wartosc"].Value = paletyZlWartosc;

                        insertPaletyZl.ExecuteNonQuery();

                        lp_paletyZl = lp_koszt;
                        lp_koszt++;
                    }
                    catch (Exception e)
                    {
                        iloscDok--;
                        MessageBox.Show("Błąd atrybutu 'Palety [zł]' \n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;
                    }

                }


                if (bonusZarzadu)
                {
                    try
                    {
                        SqlCommand selectZarzadWartosc = new SqlCommand(" if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                        + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                        + " WHERE Atk_id=281 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo >= @data  and  ath_TimeStamp <= @data) "
                                                                        + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                        + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                        + " WHERE Atk_id=281 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data else SELECt 0", connection);
                        selectZarzadWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                        selectZarzadWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                        selectZarzadWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                        selectZarzadWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                        selectZarzadWartosc.Parameters.Add("@data", SqlDbType.Int);
                        selectZarzadWartosc.Parameters["@data"].Value = dok.Data;
                        zarzadWartosc = Convert.ToDecimal(selectZarzadWartosc.ExecuteScalar().ToString());

                        SqlCommand selectZarzadWartoscZZ = new SqlCommand(" if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                       + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                       + " WHERE Atk_id=265 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo >= @data  and  ath_TimeStamp <= @data) "
                                                                       + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                       + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                       + " WHERE Atk_id=265 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data else SELECt 0", connection);
                        selectZarzadWartoscZZ.Parameters.Add("@GidNumer", SqlDbType.Int);
                        selectZarzadWartoscZZ.Parameters["@GidNumer"].Value = (int)gidNumer;
                        selectZarzadWartoscZZ.Parameters.Add("@GidTyp", SqlDbType.Int);
                        selectZarzadWartoscZZ.Parameters["@GidTyp"].Value = (int)gidTyp;
                        selectZarzadWartoscZZ.Parameters.Add("@data", SqlDbType.Int);
                        selectZarzadWartoscZZ.Parameters["@data"].Value = data;
                        zarzadWartosc += Convert.ToDecimal(selectZarzadWartoscZZ.ExecuteScalar().ToString());

                        if (zarzadWartosc == 0)
                        {
                            iloscDok--;
                            MessageBox.Show("Brak atrybutu 'Bonus zarządu [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                            zam.Wersja = XLWersja;
                            zam.Tryb = -1;
                            blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                            return;

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
                    catch(Exception e)
                    {
                        iloscDok--;
                        MessageBox.Show("Błąd atrybutu 'Bonus zarządu [%]' dla wybranej daty (Clarion: " + data + ")\n"+e.Message+"\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;
                    }

                }



                if (zyskOddzialu)
                {
                    try
                    {
                        /* SqlCommand selectZyskWartosc = new SqlCommand("SELECt SUM(Atr_Wartosc/100.0* TrE_Cena*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                        + " WHERE AtK_Nazwa = 'Zysk handlowy oddziału' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                         selectZyskWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                         selectZyskWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                         selectZyskWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                         selectZyskWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                         oddzialWartosc = Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());*/


                        SqlCommand selectZyskWartosc = new SqlCommand(" if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                        + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                        + " WHERE Atk_id=268 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo >= @data  and  ath_TimeStamp <= @data) "
                                                                        + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                        + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                        + " WHERE Atk_id=268 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data else SELECt 0", connection);
                        selectZyskWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                        selectZyskWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                        selectZyskWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                        selectZyskWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                        selectZyskWartosc.Parameters.Add("@data", SqlDbType.Int);
                        selectZyskWartosc.Parameters["@data"].Value = data;
                        oddzialWartosc = Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());


                        if (oddzialWartosc == 0)
                        {
                            iloscDok--;
                            MessageBox.Show("Brak atrybutu 'Zysk handlowy oddziału [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                            zam.Wersja = XLWersja;
                            zam.Tryb = -1;
                            blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                            return;

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
                    catch (Exception e)
                    {
                        iloscDok--;
                        MessageBox.Show("Błąd atrybutu 'Zysk handlowy oddzialu [%]' dla wybranej daty (Clarion: " + data + ")\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;
                    }

                }

                #endregion

                #region dodawanie kosztów Dod
                if (palety)
                {
                    for (int i = 0; i < lp_palety.Count; i++)
                    {
                        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                        koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyWartoscLp[i] , 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.GIDLp = lp_palety[i];
                        koszt.GIDNumer = dok.GIDNumer;
                        koszt.GIDTyp = dok.GIDTyp;
                        koszt.Wersja = XLWersja;
                        koszt.Akcja = 0;
                        koszt.Biezacy = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Inne = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Naliczono = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                        // MessageBox.Show("Koszt palet Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla palet KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }


                }

                if (transport)
                {
                    for (int i = 0; i < lp_transport.Count; i++)
                    {
                        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                        koszt.DoNaliczenia = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.GIDLp = lp_transport[i];
                        koszt.GIDNumer = dok.GIDNumer;
                        koszt.GIDTyp = dok.GIDTyp;
                        koszt.Wersja = XLWersja;
                        koszt.Akcja = 0;
                        koszt.Biezacy = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Inne = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Naliczono = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                       // MessageBox.Show("Koszt transportu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla transportu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                }

                if (paletyZl)
                {

                    XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                    koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.GIDLp = lp_paletyZl;
                    koszt.GIDNumer = dok.GIDNumer;
                    koszt.GIDTyp = dok.GIDTyp;
                    koszt.Wersja = XLWersja;
                    koszt.Akcja = 0;
                    koszt.Biezacy = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.Inne = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.Naliczono = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                    //MessageBox.Show("Koszt bonusu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (blad != 0)
                    {
                        MessageBox.Show("Błąd dodawania kosztu dodatkowego dla Palet [zł] KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                decimal wartosc = oddzialWartosc + paletyWartosc + transportWartosc + zarzadWartosc+paletyZlWartosc;
                //MessageBox.Show("Wartosc " + wartosc, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

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


               

                if ( oddzialWartosc < 0 ||  zarzadWartosc < 0)
                {
                   /*if(palety)
                    {
                        for(int i=0; i<lp_palety.Count; i++)
                        {
                            UpdateUjemne(dok, lp_palety[i], paletyWartoscLp[i], connection);
                        }
                    }

                    if (transport)
                    {
                        for (int i = 0; i < lp_transport.Count; i++)
                        {
                            UpdateUjemne(dok, lp_transport[i], transportWartoscLp[i], connection);
                        }
                    }

                    if(zyskOddzialu)
                    {
                        UpdateUjemne(dok, lp_oddzial, oddzialWartosc, connection);
                    }

                    if(bonusZarzadu)
                    {
                        UpdateUjemne(dok, lp_zarzad, zarzadWartosc, connection);
                    }*/

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


                    SqlCommand selectRoznica = new SqlCommand("SELECT (SELECT TrN_WartoscDPPrzed from cdn.TraNag where (TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer)) - " +
                                                            " (SELECT SUM(TrE_KosztKsiegowy) from cdn.TraElem where (TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer) )", connection);

                    selectRoznica.Parameters.Add("@GidNumer", SqlDbType.Int);
                    selectRoznica.Parameters["@GidNumer"].Value = dok.GIDNumer;
                    selectRoznica.Parameters.Add("@GidTyp", SqlDbType.Int);
                    selectRoznica.Parameters["@GidTyp"].Value = dok.GIDTyp;

                    double roznica = Convert.ToDouble(selectRoznica.ExecuteScalar());

                    if(roznica !=0)
                    {
                        SqlCommand updateRoznica = new SqlCommand("update e set TrE_KosztKsiegowy= Tre_KosztKsiegowy+("+roznica.ToString().Replace(',','.')+") , " +
                       "TrE_KosztRzeczywisty=Tre_KosztRzeczywisty+(" + roznica.ToString().Replace(',', '.') + ") " +
                       "from cdn.traElem as e join cdn.TraNag on TrN_GIDNumer=Tre_GIDNumer and TrN_GIDTyp= TrE_GIDTyp where TrE_GIDTyp=@GidTyp AND TrE_GIDFirma=@GidFirma AND TrE_GIDNumer =@GidNumer AND Tre_GIDlp=1", connection);

                        updateRoznica.Parameters.Add("@GidNumer", SqlDbType.Int);
                        updateRoznica.Parameters["@GidNumer"].Value = dok.GIDNumer;
                        updateRoznica.Parameters.Add("@GidTyp", SqlDbType.Int);
                        updateRoznica.Parameters["@GidTyp"].Value = dok.GIDTyp;
                        updateRoznica.Parameters.Add("@GidFirma", SqlDbType.Int);
                        updateRoznica.Parameters["@GidFirma"].Value = dok.GIDFirma;

                        updateRoznica.ExecuteNonQuery();
                    }



                    UpdateUjemneAll(dok, connection, connection_string);
                    /*SqlCommand updateTraSElem = new SqlCommand("update e set TrS_KosztKsiegowy= TrE_KosztKsiegowy, " +
                        "TrS_KosztRzeczywisty=TrE_KosztRzeczywisty " +
                        "from cdn.traSElem as e  " +
                        "join cdn.TraElem on TrE_GIDNumer=TrS_GIDNumer and TrE_GIDTyp= TrS_GIDTyp and Tre_GIDLp=TrS_GIDLp and Trs_SubGidLp=1 where TrS_GIDTyp=@GidTyp AND TrS_GIDFirma=@GidFirma AND TrS_GIDNumer =@GidNumer", connection);

                    updateTraSElem.Parameters.Add("@GidNumer", SqlDbType.Int);
                    updateTraSElem.Parameters["@GidNumer"].Value = dok.GIDNumer;
                    updateTraSElem.Parameters.Add("@GidTyp", SqlDbType.Int);
                    updateTraSElem.Parameters["@GidTyp"].Value = dok.GIDTyp;
                    updateTraSElem.Parameters.Add("@GidFirma", SqlDbType.Int);
                    updateTraSElem.Parameters["@GidFirma"].Value = dok.GIDFirma;

                    updateTraSElem.ExecuteNonQuery();*/

                   
                }

                XLZamkniecieDokumentuInfo_20221 zamDok = new XLZamkniecieDokumentuInfo_20221();
                zamDok.Wersja = XLWersja;
                zamDok.Tryb = trybZam;
                blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zamDok);
                if (blad != 0)
                {
                    MessageBox.Show("Błąd zamykania dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                connection.Close();
            }

        }








        public static void dodajDokumentSpinacz(int sesja, int XLWersja, string akronim, string numerDok, bool bonusZarzadu, bool zyskOddzialu, bool transport, bool palety, bool paletyZl, string connection_string, int trybZam, int gidNumer, int gidTyp, ref int iloscDok, string seria, List<ListaDok> doki, string numeryPZ, string frs)

        {
            int nrDok = 0;
            string typ = (bonusZarzadu == true ? " Bonus zarządu " : "") + (zyskOddzialu == true ? " Zysk handlowy oddziału " : "") + (transport == true ? " Transport " : "") + (palety == true ? " Palety " : "") + (paletyZl == true ? " Palety [zł] " : "");



            SqlConnection connection = new SqlConnection(connection_string);
            connection.Open();

            string czyIstnieje = "if exists(SELECT * FROM [CDN].[TrNOpisy] WHERE [TnO_Opis] like '" + akronim + " " + numerDok + " - " + typ + "%') SELECT 'TAK' else SELECT 'NIE'";
            SqlCommand czyIstniejeDok = new SqlCommand(czyIstnieje, connection);
            /*SqlCommand czyIstniejeDok = new SqlCommand("if exists (SELECT * FROM [CDN].[TrNOpisy] WHERE [TnO_Opis] = @opis) SELECT 'TAK' else SELECT 'NIE'", connection);
            czyIstniejeDok.Parameters.Add("@opis", SqlDbType.VarChar);
            czyIstniejeDok.Parameters["@opis"].Value = akronim + " " + numerDok + " - " + typ+" AUTOMAT";*/
            string istniejeDok = czyIstniejeDok.ExecuteScalar().ToString();


            if (istniejeDok.Trim() == "NIE")
            {

                XLDokumentNagInfo_20221 dok = new XLDokumentNagInfo_20221();
                dok.Wersja = XLWersja;
                dok.Typ = 1232;
                dok.Zwiazane = 1;
                dok.PrzyczynaKorekty = "HAN";
                dok.Seria = seria != "" ? seria : "<PUS>";
                dok.Tryb = 2;
                dok.Opis = akronim + " " + numerDok + " - " + typ+" AUTOMAT";
                dok.FRSID = Convert.ToInt32(frs);
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
                                                                   " WHERE (Atk_id=265 or Atk_id=281) and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
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



                SqlCommand paletyQuery = new SqlCommand("if exists( SELECT Tre_GIDLp FROM cdn.TraElem  WHERE TrE_TwrNazwa LIKE '%Palet%' and TrE_GIDTyp=@GidTyp AND TrE_GIDNumer=@GidNumer ) SELECT 'TAK' else SELECT 'NIE'", connection);
                paletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                paletyQuery.Parameters["@GidNumer"].Value = dok.GIDNumer;
                paletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                paletyQuery.Parameters["@GidTyp"].Value = dok.GIDTyp;
                string czyPaleta = paletyQuery.ExecuteScalar().ToString();

                // MessageBox.Show("Czy paleta: " + czyPaleta);
                if (czyPaleta.Trim() == "TAK")
                {
                    string pozycjeQueryPalety = "SELECt Tre_GIDLp FROM cdn.TraElem  WHERE TrE_TwrNazwa LIKE '%Palet%' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer; //"SELECt Tre_GIDLp FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                                                                                                                                           // " WHERE AtK_Nazwa = 'Palety' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer;
                    List<int> pozycjePalety = SelectListRowColumn(pozycjeQueryPalety, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycjePalety)
                    {
                        //MessageBox.Show("Pozycja palety: " + lp);
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
                                                                   " WHERE Atk_id=268 and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
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

                List<int> lp_transport = new List<int>();
                List<int> lp_palety = new List<int>();
                int lp_zarzad = 0;
                int lp_oddzial = 0;
                int lp_paletyZl = 0;

                int data = 0;

                decimal paletyWartosc = 0;
                List<decimal> paletyWartoscLp = new List<decimal>();
                string lpyPalety = "";
                List<string> lpyPaletySplit = new List<string>();

                decimal transportWartosc = 0;
                List<decimal> transportWartoscLp = new List<decimal>();
                string lpyTransport = "";
                List<string> lpyTransportSplit = new List<string>();

                decimal zarzadWartosc = 0;
                string lpyZarzad = "";
                List<string> lpyZarzadSplit = new List<string>();

                decimal oddzialWartosc = 0;
                decimal paletyZlWartosc = 0;
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
                        data = dok.Data;
                    }

                }


                try
                {
                    foreach(ListaDok numerPZ in doki)
                        {
                        SqlCommand LpTransportQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                           + " WHERE Atk_id=267 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                        LpTransportQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        LpTransportQuery.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                        LpTransportQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        LpTransportQuery.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                        object LpTransportObject = LpTransportQuery.ExecuteScalar();

                        if (LpTransportObject != null && LpTransportObject.ToString() != "")
                        {
                            lpyTransport = LpTransportObject.ToString();
                            lpyTransportSplit = lpyTransport.Split(',').ToList();
                        }

                        if (transport)
                        {
                            foreach (string lp in lpyTransportSplit)
                            {
                                SqlCommand selectTransportWartosc = new SqlCommand("SELECt ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                               + " WHERE Atk_id=267 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and TrE_GIDLp=@GidLp", connection);
                                selectTransportWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectTransportWartosc.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                                selectTransportWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectTransportWartosc.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                                selectTransportWartosc.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectTransportWartosc.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                transportWartoscLp.Add(Convert.ToDecimal(selectTransportWartosc.ExecuteScalar().ToString()));

                                transportWartosc += transportWartoscLp.Last();

                                SqlCommand selectLp = new SqlCommand("SELECT KDD_GIDLp from cdn.KosztyDodDok where  KDD_GIDNumer=@GidNumer  and KDD_GIDTyp =@GidTyp and KDD_ObiLp=@GidLp and KDD_ObiNumer=@ObiNumer", connection);
                                selectLp.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectLp.Parameters["@GidNumer"].Value = dok.GIDNumer;
                                selectLp.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectLp.Parameters["@GidTyp"].Value = dok.GIDTyp;
                                selectLp.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectLp.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                selectLp.Parameters.Add("@ObiNumer", SqlDbType.Int);
                                selectLp.Parameters["@ObiNumer"].Value = Convert.ToInt32(numerPZ.GIDNumer);
                                lp_transport.Add(Convert.ToInt32(selectLp.ExecuteScalar().ToString()));



                                //lp_transport.Add(lp_koszt);
                                lp_koszt++;


                                string commandUpdate = "update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1, KDD_Wartosc="+transportWartoscLp.Last().ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp =" + lp;
                                SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
                                updateCommand.ExecuteNonQuery();
                                //lp_transport.Add(lp_koszt);
                                //lp_koszt++;
                            }
                            //Update("update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1 where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")", connection_string);


                        }
                        else
                        {
                            if (LpTransportObject != null && LpTransportObject.ToString() != "")
                            {
                                string command = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")";
                                SqlCommand deleteTransport = new SqlCommand(command, connection);
                                deleteTransport.ExecuteNonQuery();
                                // DeleteFrom("delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")", connection_string);
                            }

                            string commandDelete2 = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=939";
                            //MessageBox.Show(commandDelete);
                            SqlCommand delete2 = new SqlCommand(commandDelete2, connection);
                            delete2.ExecuteNonQuery();
                        }

                        lpyTransportSplit.Clear();
                    }

                }
                catch (Exception e)
                {
                    iloscDok--;
                    MessageBox.Show("Błąd atrybutu 'Transport'\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }


                try
                {
                    foreach (ListaDok numerPZ in doki)
                    {
                        SqlCommand LpPaletyQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                       + " WHERE Atk_id=266 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                        LpPaletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        LpPaletyQuery.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                        LpPaletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        LpPaletyQuery.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                        object LpPaletyObject = LpPaletyQuery.ExecuteScalar();

                        if (LpPaletyObject != null && LpPaletyObject.ToString() != "")
                        {
                            lpyPalety = LpPaletyObject.ToString();
                            lpyPaletySplit = lpyPalety.Split(',').ToList();
                        }


                        if (palety)
                        {
                            foreach (string lp in lpyPaletySplit)
                            {
                                SqlCommand selectPaletyWartosc = new SqlCommand("SELECt ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                               + " WHERE Atk_id=266 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and TrE_GIDLp=@GidLp", connection);
                                selectPaletyWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectPaletyWartosc.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                                selectPaletyWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectPaletyWartosc.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                                selectPaletyWartosc.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectPaletyWartosc.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                paletyWartoscLp.Add(Convert.ToDecimal(selectPaletyWartosc.ExecuteScalar().ToString()));

                                paletyWartosc += paletyWartoscLp.Last();

                                SqlCommand selectLp = new SqlCommand("SELECT KDD_GIDLp from cdn.KosztyDodDok where  KDD_GIDNumer=@GidNumer  and KDD_GIDTyp =@GidTyp and KDD_ObiLp=@GidLp and KDD_ObiNumer=@ObiNumer", connection);
                                selectLp.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectLp.Parameters["@GidNumer"].Value = dok.GIDNumer;
                                selectLp.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectLp.Parameters["@GidTyp"].Value = dok.GIDTyp;
                                selectLp.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectLp.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                selectLp.Parameters.Add("@ObiNumer", SqlDbType.Int);
                                selectLp.Parameters["@ObiNumer"].Value = Convert.ToInt32(numerPZ.GIDNumer);
                                lp_palety.Add(Convert.ToInt32(selectLp.ExecuteScalar().ToString()));



                                //lp_transport.Add(lp_koszt);
                                lp_koszt++;

                                string commandUpdate = "update cdn.KosztyDodDok set KDD_Rodzaj=1669, KDD_Magazyn=1 ,KDD_Wartosc="+paletyWartoscLp.Last().ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp=" + lp;
                                SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
                                updateCommand.ExecuteNonQuery();

                                //lp_palety.Add(lp_koszt);
                                //lp_koszt++;
                            }

                            
                            // Update("update cdn.KosztyDodDok set KDD_Rodzaj=1669, KDD_Magazyn=1 where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")", connection_string);


                        }
                        else
                        {
                            if (LpPaletyObject != null && LpPaletyObject.ToString() != "")
                            {
                                string command = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")";
                                SqlCommand deletePalety = new SqlCommand(command, connection);
                                deletePalety.ExecuteNonQuery();
                            }
                            //DeleteFrom("delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")", connection_string);
                            string commandDelete2 = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=1669";
                            //MessageBox.Show(commandDelete);
                            SqlCommand delete2 = new SqlCommand(commandDelete2, connection);
                            delete2.ExecuteNonQuery();
                        }

                        lpyPaletySplit.Clear();
                    }
                }
                catch (Exception e)
                {
                    iloscDok--;
                    MessageBox.Show("Błąd atrybutu 'Palety'\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }

                string commandDelete = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=0";
                SqlCommand delete = new SqlCommand(commandDelete, connection);
                delete.ExecuteNonQuery();


               

                if (paletyZl)
                {
                    try
                    {
                        string selectPaletyZlWartosc = " SELECt SUM(Atr_Wartosc*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                     + " WHERE Atk_id=280 and TrE_GIDTyp = " + doki[0].GIDTyp + " AND TrE_GIDNumer IN (" + numeryPZ + ")";

                        paletyZlWartosc = Convert.ToDecimal(ConnectToSQL(connection_string, selectPaletyZlWartosc));



                        SqlCommand insertPaletyZl = new SqlCommand("INSERT INTO [CDN].[KosztyDodDok] ([KDD_GIDTyp] ,[KDD_GIDFirma] ,[KDD_GIDNumer] ,[KDD_GIDLp] ,[KDD_ObiTyp] ,[KDD_ObiFirma],[KDD_ObiNumer],[KDD_ObiLp]  ,[KDD_ObiDTDC] "
                                                                    + "  ,[KDD_ObiDTWO] ,[KDD_Rodzaj]  ,[KDD_Magazyn] ,[KDD_Naliczono] ,[KDD_Kod] ,[KDD_Nazwa] ,[KDD_Data] ,[KDD_Wartosc]  ,[KDD_Waluta])  VALUES (@GidTyp ,@GidFirma "
                                                                    + "  ,@GidNumer , @GidLp  , 0  ,@GidFirma ,0  , 0  , 0  , 0  , 1669  , 1   , 0   , '' , ''   ,@Data ,@Wartosc  , '')", connection);
                        insertPaletyZl.Parameters.Add("@GidNumer", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidNumer"].Value = dok.GIDNumer;
                        insertPaletyZl.Parameters.Add("@GidTyp", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidTyp"].Value = dok.GIDTyp;
                        insertPaletyZl.Parameters.Add("@GidFirma", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidFirma"].Value = dok.GIDFirma;
                        insertPaletyZl.Parameters.Add("@GidLp", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidLp"].Value = lp_koszt;
                        insertPaletyZl.Parameters.Add("@Data", SqlDbType.Int);
                        insertPaletyZl.Parameters["@Data"].Value = dok.Data;
                        insertPaletyZl.Parameters.Add("@Wartosc", SqlDbType.Decimal);
                        insertPaletyZl.Parameters["@Wartosc"].Value = paletyZlWartosc;

                        insertPaletyZl.ExecuteNonQuery();

                        lp_paletyZl = lp_koszt;
                        lp_koszt++;
                    }
                    catch (Exception e)
                    {
                        iloscDok--;
                        MessageBox.Show("Błąd atrybutu 'Palety [zł]' \n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;
                    }

                }




                if (bonusZarzadu)
                {
                    try
                    {
                        foreach (ListaDok numerPZ in doki)
                        {
                            SqlCommand LpZarzadQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                           + " WHERE Atk_id=281 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                            LpZarzadQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                            LpZarzadQuery.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                            LpZarzadQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                            LpZarzadQuery.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                            object LpZarzadObject = LpZarzadQuery.ExecuteScalar();

                            if (LpZarzadObject != null && LpZarzadObject.ToString() != "")
                            {
                                lpyZarzad = LpZarzadObject.ToString();
                                lpyZarzadSplit = lpyZarzad.Split(',').ToList();
                                data = dok.Data;

                               /* SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                           " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                dataQuery.Parameters["@GidNumer"].Value = numerPZ.GIDNumer;
                                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                dataQuery.Parameters["@GidTyp"].Value = numerPZ.GIDTyp;
                                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                                if (data == 0)
                                {
                                    data = dok.Data;
                                }*/
                            }
                            foreach (string lp in lpyZarzadSplit)
                            {
                                

                                string selectZarzadWartosc = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                          + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                          + " WHERE Atk_id=281 and TrE_GIDTyp = " + numerPZ.GIDTyp+ " AND TrE_GIDNumer =" + numerPZ.GIDNumer + " and TrE_GIDLp="+lp+" and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                          + " SELECt ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                          + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                          + " WHERE Atk_id=281 and TrE_GIDTyp = " + numerPZ.GIDTyp + " AND TrE_GIDNumer =" + numerPZ.GIDNumer + " and TrE_GIDLp=" + lp + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                                zarzadWartosc += Convert.ToDecimal(ConnectToSQL(connection_string, selectZarzadWartosc));
                            }
                            lpyZarzadSplit.Clear();



                            SqlCommand LpZarzadQuery2 = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                          + " WHERE Atk_id=265 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                            LpZarzadQuery2.Parameters.Add("@GidNumer", SqlDbType.Int);
                            LpZarzadQuery2.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                            LpZarzadQuery2.Parameters.Add("@GidTyp", SqlDbType.Int);
                            LpZarzadQuery2.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                            LpZarzadObject = LpZarzadQuery2.ExecuteScalar();

                            if (LpZarzadObject != null && LpZarzadObject.ToString() != "")
                            {
                                lpyZarzad = LpZarzadObject.ToString();
                                lpyZarzadSplit = lpyZarzad.Split(',').ToList();

                                 SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                            " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                                 dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                 dataQuery.Parameters["@GidNumer"].Value = numerPZ.GIDNumer;
                                 dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                 dataQuery.Parameters["@GidTyp"].Value = numerPZ.GIDTyp;
                                 data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                            }
                            foreach (string lp in lpyZarzadSplit)
                            {


                                string selectZarzadWartosc = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                          + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                          + " WHERE Atk_id=265 and TrE_GIDTyp = " + numerPZ.GIDTyp + " AND TrE_GIDNumer =" + numerPZ.GIDNumer + " and TrE_GIDLp=" + lp + " and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                          + " SELECt ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                          + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                          + " WHERE Atk_id=265 and TrE_GIDTyp = " + numerPZ.GIDTyp + " AND TrE_GIDNumer =" + numerPZ.GIDNumer + " and TrE_GIDLp=" + lp + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                                zarzadWartosc += Convert.ToDecimal(ConnectToSQL(connection_string, selectZarzadWartosc));
                            }
                            lpyZarzadSplit.Clear();
                        }


                        if (zarzadWartosc == 0)
                        {
                            iloscDok--;
                            MessageBox.Show("Brak atrybutu 'Bonus zarządu [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                            zam.Wersja = XLWersja;
                            zam.Tryb = -1;
                            blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                            return;

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
                    catch (Exception e)
                    {
                        iloscDok--;
                        MessageBox.Show("Błąd atrybutu 'Bonus zarządu [%]' dla wybranej daty (Clarion: " + data + ")\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;
                    }

                }



                if (zyskOddzialu)
                {
                    try
                    {

                        /* SqlCommand selectZyskWartosc = new SqlCommand("SELECt SUM(Atr_Wartosc/100.0* TrE_Cena*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                        + " WHERE AtK_Nazwa = 'Zysk handlowy oddziału' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                         selectZyskWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                         selectZyskWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                         selectZyskWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                         selectZyskWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                         oddzialWartosc = Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());*/


                        string selectZyskWartosc = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                         + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                         + " WHERE Atk_id=268 and TrE_GIDTyp = " + doki[0].GIDTyp + " AND TrE_GIDNumer IN (" + numeryPZ + ") and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                         + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                         + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                         + " WHERE Atk_id=268 and TrE_GIDTyp = " + doki[0].GIDTyp + " AND TrE_GIDNumer IN (" + numeryPZ + ") and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                        oddzialWartosc = Convert.ToDecimal(ConnectToSQL(connection_string, selectZyskWartosc));


                        if (oddzialWartosc == 0)
                        {
                            iloscDok--;
                            MessageBox.Show("Brak atrybutu 'Zysk handlowy oddziału [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                            zam.Wersja = XLWersja;
                            zam.Tryb = -1;
                            blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                            return;

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
                    catch (Exception e)
                    {
                        iloscDok--;
                        MessageBox.Show("Błąd atrybutu 'Zysk handlowy oddzialu [%]' dla wybranej daty (Clarion: " + data + ")\n" + e.Message + "\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;
                    }

                }

                #endregion

                #region dodawanie kosztów Dod
                if (palety)
                {
                    for (int i = 0; i < lp_palety.Count; i++)
                    {
                        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                        koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.GIDLp = lp_palety[i];
                        koszt.GIDNumer = dok.GIDNumer;
                        koszt.GIDTyp = dok.GIDTyp;
                        koszt.Wersja = XLWersja;
                        koszt.Akcja = 0;
                        koszt.Biezacy = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Inne = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Naliczono = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                        // MessageBox.Show("Koszt palet Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla palet KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                }

                if (transport)
                {
                    for (int i = 0; i < lp_transport.Count; i++)
                    {
                        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                        koszt.DoNaliczenia = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.GIDLp = lp_transport[i];
                        koszt.GIDNumer = dok.GIDNumer;
                        koszt.GIDTyp = dok.GIDTyp;
                        koszt.Wersja = XLWersja;
                        koszt.Akcja = 0;
                        koszt.Biezacy = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Inne = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Naliczono = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                        //MessageBox.Show("Koszt transportu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla transportu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                }

                if (paletyZl)
                {

                    XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                    koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.GIDLp = lp_paletyZl;
                    koszt.GIDNumer = dok.GIDNumer;
                    koszt.GIDTyp = dok.GIDTyp;
                    koszt.Wersja = XLWersja;
                    koszt.Akcja = 0;
                    koszt.Biezacy = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.Inne = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.Naliczono = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                    //MessageBox.Show("Koszt bonusu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (blad != 0)
                    {
                        MessageBox.Show("Błąd dodawania kosztu dodatkowego dla palet [zł] KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                decimal wartosc = oddzialWartosc + paletyWartosc + transportWartosc + zarzadWartosc+paletyZlWartosc;

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



               



                if (oddzialWartosc < 0 || zarzadWartosc < 0)
                {
                    /*if (palety)
                    {
                        for (int i = 0; i < lp_palety.Count; i++)
                        {
                            UpdateUjemne(dok, lp_palety[i], paletyWartoscLp[i], connection);
                        }
                    }

                    if (transport)
                    {
                        for (int i = 0; i < lp_transport.Count; i++)
                        {
                            UpdateUjemne(dok, lp_transport[i], transportWartoscLp[i], connection);
                        }
                    }

                    if (zyskOddzialu)
                    {
                        UpdateUjemne(dok, lp_oddzial, oddzialWartosc, connection);
                    }

                    if (bonusZarzadu)
                    {
                        UpdateUjemne(dok, lp_zarzad, zarzadWartosc, connection);
                    }*/

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


                    SqlCommand selectRoznica = new SqlCommand("SELECT (SELECT TrN_WartoscDPPrzed from cdn.TraNag where (TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer)) - " +
                                                           " (SELECT SUM(TrE_KosztKsiegowy) from cdn.TraElem where (TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer) )", connection);

                    selectRoznica.Parameters.Add("@GidNumer", SqlDbType.Int);
                    selectRoznica.Parameters["@GidNumer"].Value = dok.GIDNumer;
                    selectRoznica.Parameters.Add("@GidTyp", SqlDbType.Int);
                    selectRoznica.Parameters["@GidTyp"].Value = dok.GIDTyp;

                    double roznica = Convert.ToDouble(selectRoznica.ExecuteScalar());

                    if (roznica != 0)
                    {
                        SqlCommand updateRoznica = new SqlCommand("update e set TrE_KosztKsiegowy= Tre_KosztKsiegowy+(" + roznica.ToString().Replace(',', '.') + ") , " +
                       "TrE_KosztRzeczywisty=Tre_KosztRzeczywisty+(" + roznica.ToString().Replace(',', '.') + ") " +
                       "from cdn.traElem as e join cdn.TraNag on TrN_GIDNumer=Tre_GIDNumer and TrN_GIDTyp= TrE_GIDTyp where TrE_GIDTyp=@GidTyp AND TrE_GIDFirma=@GidFirma AND TrE_GIDNumer =@GidNumer AND Tre_GIDlp=1", connection);

                        updateRoznica.Parameters.Add("@GidNumer", SqlDbType.Int);
                        updateRoznica.Parameters["@GidNumer"].Value = dok.GIDNumer;
                        updateRoznica.Parameters.Add("@GidTyp", SqlDbType.Int);
                        updateRoznica.Parameters["@GidTyp"].Value = dok.GIDTyp;
                        updateRoznica.Parameters.Add("@GidFirma", SqlDbType.Int);
                        updateRoznica.Parameters["@GidFirma"].Value = dok.GIDFirma;

                        updateRoznica.ExecuteNonQuery();
                    }


                    UpdateUjemneAll(dok, connection, connection_string);

                    /* SqlCommand updateTraSElem = new SqlCommand("update e set TrS_KosztKsiegowy= TrE_KosztKsiegowy, " +
                         "TrS_KosztRzeczywisty=TrE_KosztRzeczywisty " +
                         "from cdn.traSElem as e  " +
                         "join cdn.TraElem on TrE_GIDNumer=TrS_GIDNumer and TrE_GIDTyp= TrS_GIDTyp and Tre_GIDLp=TrS_GIDLp and Trs_SubGidLp=1 where TrS_GIDTyp=@GidTyp AND TrS_GIDFirma=@GidFirma AND TrS_GIDNumer =@GidNumer", connection);

                     updateTraSElem.Parameters.Add("@GidNumer", SqlDbType.Int);
                     updateTraSElem.Parameters["@GidNumer"].Value = dok.GIDNumer;
                     updateTraSElem.Parameters.Add("@GidTyp", SqlDbType.Int);
                     updateTraSElem.Parameters["@GidTyp"].Value = dok.GIDTyp;
                     updateTraSElem.Parameters.Add("@GidFirma", SqlDbType.Int);
                     updateTraSElem.Parameters["@GidFirma"].Value = dok.GIDFirma;

                     updateTraSElem.ExecuteNonQuery();*/
                }

                XLZamkniecieDokumentuInfo_20221 zamDok = new XLZamkniecieDokumentuInfo_20221();
                zamDok.Wersja = XLWersja;
                zamDok.Tryb = trybZam;
                blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zamDok);
                if (blad != 0)
                {
                    MessageBox.Show("Błąd zamykania dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                connection.Close();
            }

        }



        public static void dodajDokumentMalySpinacz(int sesja, int XLWersja, string akronim, string numerDok, bool bonusZarzadu, bool zyskOddzialu, bool transport, bool palety, bool paletyZl, string connection_string, int trybZam, int gidNumer, int gidTyp, ref int iloscDok, string seria, List<ListaDok> doki, string numeryPZ, string frs)

        {
            int nrDok = 0;
            string typ = (bonusZarzadu == true ? " Bonus zarządu " : "") + (zyskOddzialu == true ? " Zysk handlowy oddziału " : "") + (transport == true ? " Transport " : "") + (palety == true ? " Palety " : "") + (paletyZl == true ? " Palety [zł] " : "");



            SqlConnection connection = new SqlConnection(connection_string);
            connection.Open();

            string czyIstnieje = "if exists(SELECT * FROM [CDN].[TrNOpisy] WHERE [TnO_Opis] like '" + akronim + " " + numerDok + " - " + typ + "%') SELECT 'TAK' else SELECT 'NIE'";
            SqlCommand czyIstniejeDok = new SqlCommand(czyIstnieje, connection);
            /*SqlCommand czyIstniejeDok = new SqlCommand("if exists (SELECT * FROM [CDN].[TrNOpisy] WHERE [TnO_Opis] = @opis) SELECT 'TAK' else SELECT 'NIE'", connection);
            czyIstniejeDok.Parameters.Add("@opis", SqlDbType.VarChar);
            czyIstniejeDok.Parameters["@opis"].Value = akronim + " " + numerDok + " - " + typ+" AUTOMAT";*/
            string istniejeDok = czyIstniejeDok.ExecuteScalar().ToString();


            if (istniejeDok.Trim() == "NIE")
            {

                XLDokumentNagInfo_20221 dok = new XLDokumentNagInfo_20221();
                dok.Wersja = XLWersja;
                dok.Typ = 1232;
                dok.Zwiazane = 1;
                dok.PrzyczynaKorekty = "HAN";
                dok.Seria = seria!=""?seria:"<PUS>";
                dok.Tryb = 2;
                dok.Opis = akronim + " " + numerDok + " - " + typ+" AUTOMAT";
                dok.FRSID = Convert.ToInt32(frs);
                int blad = cdn_api.cdn_api.XLNowyDokument(sesja, ref nrDok, dok);
                if (blad != 0)
                {
                    MessageBox.Show("Błąd dodawania dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                iloscDok++;

                #region usuwanie pozycji

                string lpQuery = "SELECT TrE_GIDLp FROM cdn.TraElem where (TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ") and " +
                                " TrE_GIDLp Not in (SELECT TrE_GIDLp from cdn.TraNag where (TrE_GIDTyp= " + dok.GIDTyp + " AND TrE_GIDNumer = " + dok.GIDNumer + ") and " + numeryPZ.Replace("GID","Zlc") + ")";
                List<int> pozycjeLp = SelectListRowColumn(lpQuery, "cdn.TraElem", connection_string);
                foreach (int lp in pozycjeLp)
                {
                    XLUsunElemInfo_20221 poz = new XLUsunElemInfo_20221();
                    poz.Pozycja = lp;
                    poz.Wersja = XLWersja;
                    blad = cdn_api.cdn_api.XLUsunPozycje(nrDok, poz);
                    if (blad != 0)
                    {
                        MessageBox.Show("Błąd usuwania pozycji nie należących do spinacza FZ dla dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    //MessageBox.Show("Usunięto pozycję: " + lp + "\nPowód: brak w (s)FZ");
                }


                if (bonusZarzadu)
                {
                    string pozycjeQuery = "SELECt Tre_GIDLp FROM cdn.TraElem where TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + " AND Tre_GIDLp NOT IN ( SELECT Tre_GIDLp from cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  " +
                                            " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                   " WHERE (Atk_id=265 or Atk_id=281) and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
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
                        //MessageBox.Show("Usunięto pozycję: " + lp + "\nPowód: brak Bonusu Zarządu");
                    }
                    
                }



                SqlCommand paletyQuery = new SqlCommand("if exists( SELECT Tre_GIDLp FROM cdn.TraElem  WHERE TrE_TwrNazwa LIKE '%Palet%' and TrE_GIDTyp=@GidTyp AND TrE_GIDNumer=@GidNumer ) SELECT 'TAK' else SELECT 'NIE'", connection);
                paletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                paletyQuery.Parameters["@GidNumer"].Value = dok.GIDNumer;
                paletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                paletyQuery.Parameters["@GidTyp"].Value = dok.GIDTyp;
                string czyPaleta = paletyQuery.ExecuteScalar().ToString();

                // MessageBox.Show("Czy paleta: " + czyPaleta);
                if (czyPaleta.Trim() == "TAK")
                {
                    string pozycjeQueryPalety = "SELECt Tre_GIDLp FROM cdn.TraElem  WHERE TrE_TwrNazwa LIKE '%Palet%' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer; //"SELECt Tre_GIDLp FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                                                                                                                                           // " WHERE AtK_Nazwa = 'Palety' and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer;
                    List<int> pozycjePalety = SelectListRowColumn(pozycjeQueryPalety, "cdn.TraElem", connection_string);
                    foreach (int lp in pozycjePalety)
                    {
                        //MessageBox.Show("Pozycja palety: " + lp);
                        XLUsunElemInfo_20221 poz = new XLUsunElemInfo_20221();
                        poz.Pozycja = lp;
                        poz.Wersja = XLWersja;
                        blad = cdn_api.cdn_api.XLUsunPozycje(nrDok, poz);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd usuwania pozycji Palety dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                       // MessageBox.Show("Usunięto pozycję: " + lp + "\nPowód: Palety");
                    }


                }

                if (zyskOddzialu)
                {
                    string pozycjeQuery = "SELECt Tre_GIDLp FROM cdn.TraElem where TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + " AND Tre_GIDLp NOT IN ( SELECT Tre_GIDLp from cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  " +
                                            " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                   " WHERE Atk_id=268 and TrE_GIDTyp=" + dok.GIDTyp + " AND TrE_GIDNumer=" + dok.GIDNumer + ")";
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
                       // MessageBox.Show("Usunięto pozycję: " + lp + "\nPowód: brak Zysku oddziału");
                    }
                    
                }


                #endregion

                int lp_koszt = 1;

                List<int> lp_transport = new List<int>();
                List<int> lp_palety = new List<int>();
                int lp_zarzad = 0;
                int lp_oddzial = 0;
                int lp_paletyZl = 0;

                int data = 0;

                decimal paletyWartosc = 0;
                List<decimal> paletyWartoscLp = new List<decimal>();
                string lpyPalety = "";
                List<string> lpyPaletySplit = new List<string>();

                decimal transportWartosc = 0;
                List<decimal> transportWartoscLp = new List<decimal>();
                string lpyTransport = "";
                List<string> lpyTransportSplit = new List<string>();

                decimal zarzadWartosc = 0;
                string lpyZarzad = "";
                List<string> lpyZarzadSplit = new List<string>();

                decimal oddzialWartosc = 0;
                decimal paletyZlWartosc = 0;
                int dzisiaj = 0;






                #region dodawanie kosztów

                try
                {

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
                            data = dok.Data;
                        }

                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Błąd daty: " + e.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }

                try
                {

                    foreach (ListaDok numerPZ in doki)
                    {
                        SqlCommand LpTransportQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                           + " WHERE Atk_id=267 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and TrE_GidLp=@GidLp", connection);
                        LpTransportQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        LpTransportQuery.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                        LpTransportQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        LpTransportQuery.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                        LpTransportQuery.Parameters.Add("@GidLp", SqlDbType.Int);
                        LpTransportQuery.Parameters["@GidLp"].Value = (int)numerPZ.GIDLp;
                        object LpTransportObject = LpTransportQuery.ExecuteScalar();

                        if (LpTransportObject != null && LpTransportObject.ToString() != "")
                        {
                            lpyTransport = LpTransportObject.ToString();
                            //MessageBox.Show("lpyTransport: " + lpyTransport);
                            lpyTransportSplit = lpyTransport.Split(',').ToList();
                        }

                        if (transport)
                        {

                            /*string selectTransportWartosc = "SELECt SUM(ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                            + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp "
                                                                            + " WHERE Atk_id=267 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi"); ;

                            transportWartosc = Convert.ToDecimal(ConnectToSQL(connection_string, selectTransportWartosc));*/

                            foreach (string lp in lpyTransportSplit)
                            {
                                SqlCommand selectTransportWartosc = new SqlCommand("SELECt ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                                + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp "
                                                                               + " WHERE Atk_id=267 and TrS_SpiTyp = @GidTyp AND TrS_SpiNumer = @GidNumer and TrS_SpiLp=@GidLp", connection);
                                selectTransportWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectTransportWartosc.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                                selectTransportWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectTransportWartosc.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                                selectTransportWartosc.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectTransportWartosc.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                transportWartoscLp.Add(Convert.ToDecimal(selectTransportWartosc.ExecuteScalar().ToString()));

                                transportWartosc += transportWartoscLp.Last();

                                SqlCommand selectLp = new SqlCommand("SELECT KDD_GIDLp from cdn.KosztyDodDok where  KDD_GIDNumer=@GidNumer  and KDD_GIDTyp =@GidTyp and KDD_ObiLp=@GidLp and KDD_ObiNumer=@ObiNumer", connection);
                                selectLp.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectLp.Parameters["@GidNumer"].Value = dok.GIDNumer;
                                selectLp.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectLp.Parameters["@GidTyp"].Value = dok.GIDTyp;
                                selectLp.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectLp.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                selectLp.Parameters.Add("@ObiNumer", SqlDbType.Int);
                                selectLp.Parameters["@ObiNumer"].Value = Convert.ToInt32(numerPZ.GIDNumer);
                                lp_transport.Add(Convert.ToInt32(selectLp.ExecuteScalar().ToString()));



                                //lp_transport.Add(lp_koszt);
                                lp_koszt++;

                                string commandUpdate = "update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1, KDD_Wartosc=" + transportWartoscLp.Last().ToString().Replace(",",".")+" where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp =" + lp ;
                                //MessageBox.Show(commandUpdate);
                                SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
                                updateCommand.ExecuteNonQuery();


                                //lp_transport.Add(lp_koszt);
                                //lp_koszt++;
                            }

                            //MessageBox.Show(commandUpdate);
                            
                            //Update("update cdn.KosztyDodDok set KDD_Rodzaj=939, KDD_Magazyn=1 where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")", connection_string);


                        }
                        else
                        {
                            if (LpTransportObject != null && LpTransportObject.ToString() != "")
                            {
                                string command = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")";
                                SqlCommand deleteTransport = new SqlCommand(command, connection);
                                deleteTransport.ExecuteNonQuery();
                            }
                            string commandDelete2 = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=939";
                            //MessageBox.Show(commandDelete);
                            SqlCommand delete2 = new SqlCommand(commandDelete2, connection);
                            delete2.ExecuteNonQuery();
                            //DeleteFrom("delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyTransport + ")", connection_string);

                        }
                        lpyTransportSplit.Clear();
                    }
                
                 }
                catch (Exception e)
            {
                MessageBox.Show("Błąd transport: " + e.Message + "\nProszę sprawdzić czy dobrze wypełniono atrybuty", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    iloscDok--;
                XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                zam.Wersja = XLWersja;
                zam.Tryb = -1;
                blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                return;
            }



                try
                {
                    foreach (ListaDok numerPZ in doki)
                    {
                        SqlCommand LpPaletyQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                       + " WHERE Atk_id=266 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and Tre_GidLp=@GidLp", connection);
                        LpPaletyQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                        LpPaletyQuery.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                        LpPaletyQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                        LpPaletyQuery.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                        LpPaletyQuery.Parameters.Add("@GidLp", SqlDbType.Int);
                        LpPaletyQuery.Parameters["@GidLp"].Value = (int)numerPZ.GIDLp;
                        object LpPaletyObject = LpPaletyQuery.ExecuteScalar();

                        if (LpPaletyObject != null && LpPaletyObject.ToString() != "")
                        {
                            lpyPalety = LpPaletyObject.ToString();
                            lpyPaletySplit = lpyPalety.Split(',').ToList();
                        }

                        if (palety)
                        {

                            /*string selectPaletyWartosc = "SELECt SUM(ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                         " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp"
                                                                           + " WHERE Atk_id=266 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi");
                            paletyWartosc = Convert.ToDecimal(ConnectToSQL(connection_string, selectPaletyWartosc));*/

                            foreach (string lp in lpyPaletySplit)
                            {
                                SqlCommand selectPaletyWartosc = new SqlCommand("SELECt ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                                +" join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp "
                                                                               + " WHERE Atk_id=266 and TrS_SpiTyp = @GidTyp AND Trs_SpiNumer = @GidNumer and TrS_SpiLp=@GidLp", connection);
                                selectPaletyWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectPaletyWartosc.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                                selectPaletyWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectPaletyWartosc.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                                selectPaletyWartosc.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectPaletyWartosc.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                paletyWartoscLp.Add(Convert.ToDecimal(selectPaletyWartosc.ExecuteScalar().ToString()));

                                paletyWartosc += paletyWartoscLp.Last();


                                SqlCommand selectLp = new SqlCommand("SELECT KDD_GIDLp from cdn.KosztyDodDok where  KDD_GIDNumer=@GidNumer  and KDD_GIDTyp =@GidTyp and KDD_ObiLp=@GidLp and KDD_ObiNumer=@ObiNumer", connection);
                                selectLp.Parameters.Add("@GidNumer", SqlDbType.Int);
                                selectLp.Parameters["@GidNumer"].Value = dok.GIDNumer;
                                selectLp.Parameters.Add("@GidTyp", SqlDbType.Int);
                                selectLp.Parameters["@GidTyp"].Value = dok.GIDTyp;
                                selectLp.Parameters.Add("@GidLp", SqlDbType.Int);
                                selectLp.Parameters["@GidLp"].Value = Convert.ToInt32(lp);
                                selectLp.Parameters.Add("@ObiNumer", SqlDbType.Int);
                                selectLp.Parameters["@ObiNumer"].Value = Convert.ToInt32(numerPZ.GIDNumer);
                                lp_palety.Add(Convert.ToInt32(selectLp.ExecuteScalar().ToString()));



                                //lp_transport.Add(lp_koszt);
                                lp_koszt++;


                                string commandUpdate = "update cdn.KosztyDodDok set KDD_Rodzaj=1669, KDD_Magazyn=1, KDD_Wartosc="+paletyWartoscLp.Last().ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp =" + lp ;
                                //MessageBox.Show(commandUpdate);
                                SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
                                updateCommand.ExecuteNonQuery();
                                // lp_palety.Add(lp_koszt);
                                // lp_koszt++;
                            }

                            
                            //Update("update cdn.KosztyDodDok set KDD_Rodzaj=1669, KDD_Magazyn=1 where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")", connection_string);


                        }
                        else
                        {
                            if (LpPaletyObject != null && LpPaletyObject.ToString() != "")
                            {
                                string command = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")";
                                SqlCommand deletePalety = new SqlCommand(command, connection);
                                deletePalety.ExecuteNonQuery();
                            }
                            string commandDelete2 = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=1669";
                            //MessageBox.Show(commandDelete);
                            SqlCommand delete2 = new SqlCommand(commandDelete2, connection);
                            delete2.ExecuteNonQuery();
                            //DeleteFrom("delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_ObiLp IN (" + lpyPalety + ")", connection_string);

                        }

                        lpyPaletySplit.Clear();
                    }
                } 
                catch (Exception e)
            {
                    iloscDok--;
                MessageBox.Show("Błąd Palety: " + e.Message + "\nProszę sprawdzić czy dobrze wypełniono atrybuty", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                zam.Wersja = XLWersja;
                zam.Tryb = -1;
                blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                return;
            }


                string commandDelete = "delete from cdn.KosztyDodDok  where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_Rodzaj=0";
                //MessageBox.Show(commandDelete);
                SqlCommand delete = new SqlCommand(commandDelete, connection);
                delete.ExecuteNonQuery();




                try
                {
                    if (paletyZl)
                    {


                        string selectPaletyZlWartosc = "SELECt SUM(Atr_Wartosc*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                         " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp"
                                                                           + " WHERE Atk_id=280 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi"); 

                        // MessageBox.Show("Zarząd wartość:" + selectZarzadWartosc);
                        paletyZlWartosc = Convert.ToDecimal(ConnectToSQL(connection_string, selectPaletyZlWartosc));



                        SqlCommand insertPaletyZl = new SqlCommand("INSERT INTO [CDN].[KosztyDodDok] ([KDD_GIDTyp] ,[KDD_GIDFirma] ,[KDD_GIDNumer] ,[KDD_GIDLp] ,[KDD_ObiTyp] ,[KDD_ObiFirma],[KDD_ObiNumer],[KDD_ObiLp]  ,[KDD_ObiDTDC] "
                                                                    + "  ,[KDD_ObiDTWO] ,[KDD_Rodzaj]  ,[KDD_Magazyn] ,[KDD_Naliczono] ,[KDD_Kod] ,[KDD_Nazwa] ,[KDD_Data] ,[KDD_Wartosc]  ,[KDD_Waluta])  VALUES (@GidTyp ,@GidFirma "
                                                                    + "  ,@GidNumer , @GidLp  , 0  ,@GidFirma ,0  , 0  , 0  , 0  , 1669  , 1   , 0   , '' , ''   ,@Data ,@Wartosc  , '')", connection);
                        insertPaletyZl.Parameters.Add("@GidNumer", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidNumer"].Value = dok.GIDNumer;
                        insertPaletyZl.Parameters.Add("@GidTyp", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidTyp"].Value = dok.GIDTyp;
                        insertPaletyZl.Parameters.Add("@GidFirma", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidFirma"].Value = dok.GIDFirma;
                        insertPaletyZl.Parameters.Add("@GidLp", SqlDbType.Int);
                        insertPaletyZl.Parameters["@GidLp"].Value = lp_koszt;
                        insertPaletyZl.Parameters.Add("@Data", SqlDbType.Int);
                        insertPaletyZl.Parameters["@Data"].Value = dok.Data;
                        insertPaletyZl.Parameters.Add("@Wartosc", SqlDbType.Decimal);
                        insertPaletyZl.Parameters["@Wartosc"].Value = paletyZlWartosc;

                        insertPaletyZl.ExecuteNonQuery();
                        //MessageBox.Show(insertZarzad.CommandText);


                        lp_paletyZl = lp_koszt;
                        lp_koszt++;

                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Błąd Palet [zł]: " + e.Message + "\nProszę sprawdzić czy dobrze wypełniono atrybuty", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    iloscDok--;
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }






                try
                { 
            if (bonusZarzadu)
                {
                        foreach (ListaDok numerPZ in doki)
                        {
                            SqlCommand LpZarzadQuery = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                           + " WHERE Atk_id=281 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and Tre_GidLp=@GidLp", connection);
                            LpZarzadQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                            LpZarzadQuery.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                            LpZarzadQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                            LpZarzadQuery.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                            LpZarzadQuery.Parameters.Add("@GidLp", SqlDbType.Int);
                            LpZarzadQuery.Parameters["@GidLp"].Value = (int)numerPZ.GIDLp;
                            object LpZarzadObject = LpZarzadQuery.ExecuteScalar();

                            if (LpZarzadObject != null && LpZarzadObject.ToString() != "")
                            {
                                lpyZarzad = LpZarzadObject.ToString();
                                lpyZarzadSplit = lpyZarzad.Split(',').ToList();
                                data = dok.Data;

                               /* SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                          " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                dataQuery.Parameters["@GidNumer"].Value = numerPZ.GIDNumer;
                                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                dataQuery.Parameters["@GidTyp"].Value = numerPZ.GIDTyp;
                                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                                if (data == 0)
                                {
                                    data = dok.Data;
                                }*/
                            }
                            foreach (string lp in lpyZarzadSplit)
                            {

                                string selectZarzadWartosc = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                      + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                      + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                      + " WHERE Atk_id=281 and TrS_SpiTyp = " + numerPZ.GIDTyp + " AND TrS_SpiNumer=" + numerPZ.GIDNumer + " AND TrS_SpiLp=" + lp + " and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                      + " SELECt ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                      + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                      + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                      + " WHERE Atk_id=281 and TrS_SpiTyp = " + numerPZ.GIDTyp + " AND TrS_SpiNumer=" + numerPZ.GIDNumer + " AND TrS_SpiLp=" + lp + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                                // MessageBox.Show("Zarząd wartość:" + selectZarzadWartosc);
                                zarzadWartosc += Convert.ToDecimal(ConnectToSQL(connection_string, selectZarzadWartosc));
                                //MessageBox.Show("Zarząd wartość:" + zarzadWartosc);

                            }

                            lpyZarzadSplit.Clear();



                            SqlCommand LpZarzadQuery2 = new SqlCommand("SELECt STRING_AGG(TrE_GIDLp, ',') FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                          + " WHERE Atk_id=265 and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and Tre_GidLp=@GidLp", connection);
                            LpZarzadQuery2.Parameters.Add("@GidNumer", SqlDbType.Int);
                            LpZarzadQuery2.Parameters["@GidNumer"].Value = (int)numerPZ.GIDNumer;
                            LpZarzadQuery2.Parameters.Add("@GidTyp", SqlDbType.Int);
                            LpZarzadQuery2.Parameters["@GidTyp"].Value = (int)numerPZ.GIDTyp;
                            LpZarzadQuery2.Parameters.Add("@GidLp", SqlDbType.Int);
                            LpZarzadQuery2.Parameters["@GidLp"].Value = (int)numerPZ.GIDLp;
                            LpZarzadObject = LpZarzadQuery2.ExecuteScalar();

                            if (LpZarzadObject != null && LpZarzadObject.ToString() != "")
                            {
                                lpyZarzad = LpZarzadObject.ToString();
                                lpyZarzadSplit = lpyZarzad.Split(',').ToList();

                                SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                           " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                                 dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                                 dataQuery.Parameters["@GidNumer"].Value = numerPZ.GIDNumer;
                                 dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                                 dataQuery.Parameters["@GidTyp"].Value = numerPZ.GIDTyp;
                                 data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                                 
                            }
                            foreach (string lp in lpyZarzadSplit)
                            {

                                string selectZarzadWartosc = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                      + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                      + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                      + " WHERE Atk_id=265 and TrS_SpiTyp = " + numerPZ.GIDTyp + " AND TrS_SpiNumer=" + numerPZ.GIDNumer + " AND TrS_SpiLp=" + lp + " and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                      + " SELECt ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                      + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                      + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                      + " WHERE Atk_id=265 and TrS_SpiTyp = " + numerPZ.GIDTyp + " AND TrS_SpiNumer=" + numerPZ.GIDNumer + " AND TrS_SpiLp=" + lp + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                               // MessageBox.Show("Zarząd wartość 2:" + selectZarzadWartosc);
                               // MessageBox.Show("Zarząd wartość 2:" + ConnectToSQL(connection_string, selectZarzadWartosc));
                                zarzadWartosc += Convert.ToDecimal(ConnectToSQL(connection_string, selectZarzadWartosc));
                               // MessageBox.Show("Zarząd wartość 2:" + zarzadWartosc);
                            }

                            lpyZarzadSplit.Clear();
                        }
                        

                    if (zarzadWartosc == 0)
                    {

                        MessageBox.Show("Brak atrybutu 'Bonus zarządu [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            iloscDok--;
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;

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

                      //  MessageBox.Show(insertZarzad.CommandText);

                        insertZarzad.ExecuteNonQuery();
                            //MessageBox.Show(insertZarzad.CommandText);
                        

                    lp_zarzad = lp_koszt;
                    lp_koszt++;

                }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Błąd bonusu zarządu: " + e.Message+"\nProszę sprawdzić czy dobrze wypełniono atrybuty", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    iloscDok--;
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }



                try {
                if (zyskOddzialu)
                {

                    /* SqlCommand selectZyskWartosc = new SqlCommand("SELECt SUM(Atr_Wartosc/100.0* TrE_Cena*TrE_Ilosc) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID "
                                                                    + " WHERE AtK_Nazwa = 'Zysk handlowy oddziału' and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer", connection);
                     selectZyskWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                     selectZyskWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                     selectZyskWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                     selectZyskWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                     oddzialWartosc = Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());*/


                    string selectZyskWartosc = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                     + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                     + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                     + " WHERE Atk_id=268 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi") + " and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                     + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                     + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                     + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                     + " WHERE Atk_id=268 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi") + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                    oddzialWartosc = Convert.ToDecimal(ConnectToSQL(connection_string, selectZyskWartosc));


                    if (oddzialWartosc == 0)
                    {

                        MessageBox.Show("Brak atrybutu 'Zysk handlowy oddziału [%]' dla wybranej daty (Clarion: " + data + ")\nUsunięto dokument KDZ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                        zam.Wersja = XLWersja;
                        zam.Tryb = -1;
                        blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                        return;

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
                }
                catch (Exception e)
                {
                    MessageBox.Show("Błąd zysku handlowego oddziału: " + e.Message + "\nProszę sprawdzić czy dobrze wypełniono atrybuty", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    iloscDok--;
                    XLZamkniecieDokumentuInfo_20221 zam = new XLZamkniecieDokumentuInfo_20221();
                    zam.Wersja = XLWersja;
                    zam.Tryb = -1;
                    blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zam);
                    return;
                }

                #endregion

                #region dodawanie kosztów Dod
                if (palety)
                {
                    for (int i = 0; i < lp_palety.Count; i++)
                    {
                        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                        koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.GIDLp = lp_palety[i];
                        koszt.GIDNumer = dok.GIDNumer;
                        koszt.GIDTyp = dok.GIDTyp;
                        koszt.Wersja = XLWersja;
                        koszt.Akcja = 0;
                        koszt.Biezacy = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Inne = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Naliczono = Convert.ToString(Math.Round(paletyWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                        // MessageBox.Show("Koszt palet Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla palet KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                }

                if (transport)
                {
                    for (int i = 0; i < lp_transport.Count; i++)
                    {


                        XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                        koszt.DoNaliczenia = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.GIDLp = lp_transport[i];
                        koszt.GIDNumer = dok.GIDNumer;
                        koszt.GIDTyp = dok.GIDTyp;
                        koszt.Wersja = XLWersja;
                        koszt.Akcja = 0;
                        koszt.Biezacy = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Inne = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        koszt.Naliczono = Convert.ToString(Math.Round(transportWartoscLp[i], 2, MidpointRounding.AwayFromZero) * 10);
                        blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                        //MessageBox.Show("Koszt transportu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        if (blad != 0)
                        {
                            MessageBox.Show("Błąd dodawania kosztu dodatkowego dla transportu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                }


                if (paletyZl)
                {

                    XLKosztDodDokInfo_20221 koszt = new XLKosztDodDokInfo_20221();
                    koszt.DoNaliczenia = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.GIDLp = lp_paletyZl;
                    koszt.GIDNumer = dok.GIDNumer;
                    koszt.GIDTyp = dok.GIDTyp;
                    koszt.Wersja = XLWersja;
                    koszt.Akcja = 0;
                    koszt.Biezacy = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.Inne = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    koszt.Naliczono = Convert.ToString(Math.Round(paletyZlWartosc, 2, MidpointRounding.AwayFromZero) * 10);
                    blad = cdn_api.cdn_api.XLKosztyDodDok(koszt);
                    //MessageBox.Show("Koszt bonusu Do NAliczenia " + koszt.DoNaliczenia + "\nlp " + koszt.GIDLp + "\ngid numer " + koszt.GIDNumer + "\ntyp " + koszt.GIDTyp + "\nakcja " + koszt.Akcja + "\nnaliczono " + koszt.Naliczono + "\nInne " + koszt.Inne + "\nbieżące: " + koszt.Biezacy, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (blad != 0)
                    {
                        MessageBox.Show("Błąd dodawania kosztu dodatkowego dla palet [zł] KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    koszt.DoNaliczenia =  Convert.ToString(Math.Round(oddzialWartosc, 2, MidpointRounding.AwayFromZero) * 10);
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

                decimal wartosc = oddzialWartosc + paletyWartosc + transportWartosc + zarzadWartosc+paletyZlWartosc;

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

              /*  SqlCommand updateKDD = new SqlCommand("update cdn.KosztyDodDok set KDD_Naliczono=0 where KDD_GIDTyp=@GidTyp AND KDD_GIDFirma=@GidFirma AND KDD_GIDNumer =@GidNumer", connection);

                updateKDD.Parameters.Add("@GidNumer", SqlDbType.Int);
                updateKDD.Parameters["@GidNumer"].Value = dok.GIDNumer;
                updateKDD.Parameters.Add("@GidTyp", SqlDbType.Int);
                updateKDD.Parameters["@GidTyp"].Value = dok.GIDTyp;
                updateKDD.Parameters.Add("@GidFirma", SqlDbType.Int);
                updateKDD.Parameters["@GidFirma"].Value = dok.GIDFirma;

                updateKDD.ExecuteNonQuery();*/



                #endregion



                XLKosztDodDokInfo_20221 koszt2 = new XLKosztDodDokInfo_20221();
                koszt2.Wersja = XLWersja;
                koszt2.Akcja = -1;
                blad = cdn_api.cdn_api.XLKosztyDodDok(koszt2);
                if (blad != 0)
                {
                    MessageBox.Show("Błąd czyszczenia kosztów dodatkowych KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }


               
                if (oddzialWartosc < 0 || zarzadWartosc < 0)
                {
                   /* if (palety)
                    {
                        for (int i = 0; i < lp_palety.Count; i++)
                        {
                            UpdateUjemne(dok, lp_palety[i], paletyWartoscLp[i], connection);
                        }
                    }

                    if (transport)
                    {
                        for (int i = 0; i < lp_transport.Count; i++)
                        {
                            UpdateUjemne(dok, lp_transport[i], transportWartoscLp[i], connection);
                        }
                    }

                    if (zyskOddzialu)
                    {
                        UpdateUjemne(dok, lp_oddzial, oddzialWartosc, connection);
                    }

                    if (bonusZarzadu)
                    {
                        UpdateUjemne(dok, lp_zarzad, zarzadWartosc, connection);
                    }*/

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


                    SqlCommand selectRoznica = new SqlCommand("SELECT (SELECT TrN_WartoscDPPrzed from cdn.TraNag where (TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer)) - " +
                                                           " (SELECT SUM(TrE_KosztKsiegowy) from cdn.TraElem where (TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer) )", connection);

                    selectRoznica.Parameters.Add("@GidNumer", SqlDbType.Int);
                    selectRoznica.Parameters["@GidNumer"].Value = dok.GIDNumer;
                    selectRoznica.Parameters.Add("@GidTyp", SqlDbType.Int);
                    selectRoznica.Parameters["@GidTyp"].Value = dok.GIDTyp;

                    double roznica = Convert.ToDouble(selectRoznica.ExecuteScalar());

                    if (roznica != 0)
                    {
                        SqlCommand updateRoznica = new SqlCommand("update e set TrE_KosztKsiegowy= Tre_KosztKsiegowy+(" + roznica.ToString().Replace(',', '.') + ") , " +
                       "TrE_KosztRzeczywisty=Tre_KosztRzeczywisty+(" + roznica.ToString().Replace(',', '.') + ") " +
                       "from cdn.traElem as e join cdn.TraNag on TrN_GIDNumer=Tre_GIDNumer and TrN_GIDTyp= TrE_GIDTyp where TrE_GIDTyp=@GidTyp AND TrE_GIDFirma=@GidFirma AND TrE_GIDNumer =@GidNumer AND Tre_GIDlp=1", connection);

                        updateRoznica.Parameters.Add("@GidNumer", SqlDbType.Int);
                        updateRoznica.Parameters["@GidNumer"].Value = dok.GIDNumer;
                        updateRoznica.Parameters.Add("@GidTyp", SqlDbType.Int);
                        updateRoznica.Parameters["@GidTyp"].Value = dok.GIDTyp;
                        updateRoznica.Parameters.Add("@GidFirma", SqlDbType.Int);
                        updateRoznica.Parameters["@GidFirma"].Value = dok.GIDFirma;

                        updateRoznica.ExecuteNonQuery();
                    }

                    UpdateUjemneAll(dok, connection, connection_string);



                    /*SqlCommand updateTraSElem = new SqlCommand("update e set TrS_KosztKsiegowy= TrE_KosztKsiegowy, " +
                        "TrS_KosztRzeczywisty=TrE_KosztRzeczywisty " +
                        "from cdn.traSElem as e  " +
                        "join cdn.TraElem on TrE_GIDNumer=TrS_GIDNumer and TrE_GIDTyp= TrS_GIDTyp and Tre_GIDLp=TrS_GIDLp and Trs_SubGidLp=1 where TrS_GIDTyp=@GidTyp AND TrS_GIDFirma=@GidFirma AND TrS_GIDNumer =@GidNumer", connection);

                    updateTraSElem.Parameters.Add("@GidNumer", SqlDbType.Int);
                    updateTraSElem.Parameters["@GidNumer"].Value = dok.GIDNumer;
                    updateTraSElem.Parameters.Add("@GidTyp", SqlDbType.Int);
                    updateTraSElem.Parameters["@GidTyp"].Value = dok.GIDTyp;
                    updateTraSElem.Parameters.Add("@GidFirma", SqlDbType.Int);
                    updateTraSElem.Parameters["@GidFirma"].Value = dok.GIDFirma;

                    updateTraSElem.ExecuteNonQuery();*/
                }


                XLZamkniecieDokumentuInfo_20221 zamDok = new XLZamkniecieDokumentuInfo_20221();
                zamDok.Wersja = XLWersja;
                zamDok.Tryb = trybZam;
                blad = cdn_api.cdn_api.XLZamknijDokument(nrDok, zamDok);
                if (blad != 0)
                {
                    MessageBox.Show("Błąd zamykania dokumentu KDZ: " + blad, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                connection.Close();
            }

           

        }


        public static void WykonajKDZ(string connection_string, string gidNumery, string stan, int gidNumer, int gidTyp, List<ListaDok> doki, SqlConnection connection, int XLWersja, int sesja, string numerDok, ref int l, string frs)
        {
            string czyIstniejeQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                " WHERE(Atk_id=265 or Atk_id=268 or Atk_id=267 or Atk_id=266 or Atk_id=280 or Atk_id=281) and TrN_GIDNumer IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                                "ELSE SELECT 'NIE'";
            string czyIstnieje = ConnectToSQL(connection_string, czyIstniejeQuery);

            string czyCenaQuery = "if exists (SELECT * FROM cdn.TraNag join cdn.TraElem on TrN_GIDTyp=TrE_GIDTyp and TrN_GIDNumer=TrE_GIDNumer where TRE_Cena = 0 and TrE_GIDTyp = " + doki[0].GIDTyp + " AND TrN_GIDNumer IN (" + gidNumery + ") )" +
                                " SELECT 'TAK' else SELECT 'NIE'";
            string czyCenaZero = ConnectToSQL(connection_string, czyCenaQuery);


            string iloscQuery = "SELECT (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                        " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + " and Twr_Typ != 4) - " +
                                                        " (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                        " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                        " WHERE (Atk_id=266 or Atk_id=280)  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ")";

            int ilosc = Convert.ToInt32(ConnectToSQL(connection_string, iloscQuery));

            if(ilosc==0)
            {
                 iloscQuery = "if (SELECT STRING_AGG(CONVERT( varchar(15), Tre_GIDNumer) +CONVERT( varchar(5), Tre_GIDLp),',') from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                       " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + " and Twr_Typ != 4) = " +
                                                       " (SELECT STRING_AGG(CONVERT( varchar(15), Tre_GIDNumer) +CONVERT( varchar(5), Tre_GIDLp),',') from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                       " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                       " WHERE (Atk_id=266 or Atk_id=280)  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 0 else SELECT 1";

                 ilosc = Convert.ToInt32(ConnectToSQL(connection_string, iloscQuery));
            }

            //MessageBox.Show("czy Istnieje: " + czyIstnieje+ "ilosc: " + ilosc);

            if (czyIstnieje == "TAK" && stan=="3" && ilosc > 0 && czyCenaZero=="NIE")// || (stan=="2" && Bufor.CheckedRaw=="0")))
            {

               // MessageBox.Show("Istnieje atrybut", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

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


                decimal bonusWartosc = 0;
                decimal oddzialWartosc = 0;


                string czyBonusZarzaduQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                        " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                        " WHERE (Atk_id=265 or Atk_id=281)  and  TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                        "ELSE SELECT 'NIE'";
                bool czyBonusZarzadu = ConnectToSQL(connection_string, czyBonusZarzaduQuery).Trim() == "TAK";


                if (czyBonusZarzadu == true)
                {
                    bonusWartosc = DodDok.WyliczBonusSpinacz(connection, doki, gidNumery, gidTyp, gidNumer);
                   // MessageBox.Show("Bonus zarządu wartość: " + bonusWartosc);
                }


                string czyZyskOddzialuQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                       " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                       " WHERE Atk_id=268  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                       "ELSE SELECT 'NIE'";
                bool czyZyskOddzialu = ConnectToSQL(connection_string, czyZyskOddzialuQuery).Trim() == "TAK";

                if (czyZyskOddzialu == true)
                {
                    oddzialWartosc = DodDok.WyliczOddzialSpinacz(connection, doki, gidNumery, gidTyp, gidNumer);
                    // MessageBox.Show("ZyskOddziału wartość: " + oddzialWartosc);
                }


                string czyTransportQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                        " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                        " WHERE Atk_id=267  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                        "ELSE SELECT 'NIE'";
                bool czyTransport = ConnectToSQL(connection_string, czyTransportQuery).Trim() == "TAK";

                string czyPaletyQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                       " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                       " WHERE Atk_id=266  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                       "ELSE SELECT 'NIE'";
                bool czyPalety = ConnectToSQL(connection_string, czyPaletyQuery).Trim() == "TAK";



                string czyPaletyZlQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                       " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                       " WHERE Atk_id=280  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                       "ELSE SELECT 'NIE'";
                bool czyPaletyZl = ConnectToSQL(connection_string, czyPaletyZlQuery).Trim() == "TAK";

                string iloscBonusZarzaduQuery = "SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                        " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                         " WHERE Atk_id=265  and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp;
                int iloscBonusZarzadu = Convert.ToInt32(ConnectToSQL(connection_string, iloscBonusZarzaduQuery));


                string iloscZyskOddzialuQuery = "SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                        " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                         " WHERE Atk_id=268   and TrN_GIDNumer  IN (" + gidNumery + ") and TrN_GIDTyp = " + doki[0].GIDTyp;
                int iloscZyskOddzialu = Convert.ToInt32(ConnectToSQL(connection_string, iloscZyskOddzialuQuery));







                /*SqlCommand typQuery = new SqlCommand("SELECT TOP (1) AtK_Nazwa from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                        " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                        " WHERE(AtK_Nazwa = 'Bonus zarządu' or Atk_Nazwa = 'Zysk handlowy oddziału' or Atk_Nazwa = 'Transport' or Atk_Nazwa = 'Palety') and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                typQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                typQuery.Parameters["@GidNumer"].Value = gidNumer;
                typQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                typQuery.Parameters["@GidTyp"].Value = gidTyp;
                string typ = typQuery.ExecuteScalar().ToString();*/


                foreach (ListaDok dok in doki)
                {
                    XLZwiazanyDokInfo_20221 zwiazany = new XLZwiazanyDokInfo_20221();
                    zwiazany.Wersja = XLWersja;
                    zwiazany.GIDTyp = dok.GIDTyp;
                    zwiazany.GIDNumer = dok.GIDNumer;
                    int blad2 = cdn_api.cdn_api.XLDokumentyZwiazane(zwiazany);
                    if (blad2 != 0)
                    {
                        MessageBox.Show("Błąd dodawania dokumentu związanego: " + blad2, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                //int l = 0;


                if (czyTransport == true || czyPalety == true || czyPaletyZl==true)
                {
                    if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                    {
                        if (iloscBonusZarzadu == iloscZyskOddzialu && (bonusWartosc >= 0 && oddzialWartosc >= 0))
                        {
                            if (iloscBonusZarzadu == ilosc)
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            else
                            {
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            }
                        }
                        else
                        {
                            if (iloscBonusZarzadu == ilosc && bonusWartosc >= 0)
                            {
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            }
                            else if (iloscZyskOddzialu == ilosc && oddzialWartosc >= 0)
                            {
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            }
                            else
                            {
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            }
                        }
                    }
                    else
                    {
                        if (czyBonusZarzadu == true)
                        {
                            if (iloscBonusZarzadu == ilosc && bonusWartosc >= 0)
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            else
                            {
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            }
                        }
                        else if (czyZyskOddzialu == true)
                        {
                            if (iloscZyskOddzialu == ilosc && oddzialWartosc >= 0)
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            else
                            {
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            }

                        }
                        else
                            DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                    }

                }
                else if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                {
                    if (iloscBonusZarzadu == iloscZyskOddzialu && ((bonusWartosc >= 0 && oddzialWartosc >= 0) || (bonusWartosc < 0 && oddzialWartosc < 0)))
                        DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                    else
                    {
                        DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                        DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                    }
                }
                else
                {
                    DodDok.dodajDokumentSpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl,  connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                }






            }
        
            else if(czyCenaZero=="TAK")
            {
                MessageBox.Show("W fakturze występują ceny 0zł. Anulowano dodawanie KDZ ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }
}




        public static void WykonajKDZMalySpinacz(string connection_string, string gidNumery, string stan, int gidNumer, int gidTyp, List<ListaDok> doki, SqlConnection connection, int XLWersja, int sesja, string numerDok, ref int l, string frs)
        {
            try
            {
                string czyCenaQuery = "if exists (SELECT * FROM cdn.TraNag join cdn.TraElem on TrN_GIDTyp=TrE_GIDTyp and TrN_GIDNumer=TrE_GIDNumer where TRE_Cena = 0 and TrE_GIDTyp = " + gidTyp + " AND TrE_GIDNumer= " + gidNumer + " )" +
                                    " SELECT 'TAK' else SELECT 'NIE'";
                string czyCenaZero = ConnectToSQL(connection_string, czyCenaQuery);

                string czyIstniejeQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                                    " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                                    " WHERE(Atk_id=265 or Atk_id=268 or Atk_id=267 or Atk_id=266 or Atk_id=280 or Atk_id=281) and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                                    "ELSE SELECT 'NIE'";
                // MessageBox.Show(czyIstniejeQuery);
                string czyIstnieje = ConnectToSQL(connection_string, czyIstniejeQuery);


                string iloscQuery = "SELECT (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                            " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + " and Twr_Typ != 4) - " +
                                                            " (SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                            " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                            " WHERE (Atk_id=266 or Atk_id=280) and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ")";

                int ilosc = Convert.ToInt32(ConnectToSQL(connection_string, iloscQuery));

                if (ilosc == 0)
                {
                    iloscQuery = "if (SELECT STRING_AGG(CONVERT( varchar(15), Tre_GIDNumer) +CONVERT( varchar(5), Tre_GIDLp),',') from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                            " join cdn.TwrKarty on Twr_Kod = TrE_TwrKod  WHERE " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + " and Twr_Typ != 4) = " +
                                                            " (SELECT STRING_AGG(CONVERT( varchar(15), Tre_GIDNumer) +CONVERT( varchar(5), Tre_GIDLp),',') from cdn.TraNag join cdn.TraElem on TrN_GIDNumer = TrE_GIDNumer and TrN_GIDTyp = Tre_GIDTyp " +
                                                            " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                            " WHERE (Atk_id=266 or Atk_id=280)  and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 0 else SELECT 1";

                    ilosc = Convert.ToInt32(ConnectToSQL(connection_string, iloscQuery));
                }

                //MessageBox.Show(iloscQuery);

                // MessageBox.Show("czy Istnieje: " + czyIstnieje+ "ilosc: " + ilosc);

                if (czyIstnieje == "TAK" && stan == "3" && ilosc > 0 && czyCenaZero == "NIE")// || (stan=="2" && Bufor.CheckedRaw=="0")))
                {

                    // MessageBox.Show("Istnieje atrybut", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Information);

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


                    decimal oddzialWartosc = 0;
                    decimal bonusWartosc = 0;


                    string czyBonusZarzaduQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                            " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                            " WHERE (Atk_id=265 or Atk_id=281) and  " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                            "ELSE SELECT 'NIE'";
                    bool czyBonusZarzadu = ConnectToSQL(connection_string, czyBonusZarzaduQuery).Trim() == "TAK";

                    if (czyBonusZarzadu == true)
                    {
                        bonusWartosc = DodDok.WyliczBonusMalySpinacz(connection, doki, gidNumery, gidTyp, gidNumer);
                       // MessageBox.Show("Bonus zarządu wartość: " + bonusWartosc);
                    }




                    string czyZyskOddzialuQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                           " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                           " WHERE Atk_id=268  and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                           "ELSE SELECT 'NIE'";
                    bool czyZyskOddzialu = ConnectToSQL(connection_string, czyZyskOddzialuQuery).Trim() == "TAK";

                    if (czyZyskOddzialu == true)
                    {
                        oddzialWartosc = DodDok.WyliczOddzialMalySpinacz(connection, doki, gidNumery, gidTyp, gidNumer);
                       // MessageBox.Show("ZyskOddziału wartość: " + oddzialWartosc);
                    }

                    string czyTransportQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                            " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                            " WHERE Atk_id=267  and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                            "ELSE SELECT 'NIE'";
                    bool czyTransport = ConnectToSQL(connection_string, czyTransportQuery).Trim() == "TAK";

                    string czyPaletyQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                           " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                           " WHERE Atk_id=266  and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                           "ELSE SELECT 'NIE'";
                    bool czyPalety = ConnectToSQL(connection_string, czyPaletyQuery).Trim() == "TAK";

                    string czyPaletyZlQuery = "if exists(SELECT * from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                           " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                           " WHERE Atk_id=280  and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp + ") SELECT 'TAK' " +
                                                           "ELSE SELECT 'NIE'";
                    bool czyPaletyZl = ConnectToSQL(connection_string, czyPaletyZlQuery).Trim() == "TAK";

                    string iloscBonusZarzaduQuery = "SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                            " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                             " WHERE (Atk_id=265 or Atk_id=281)  and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp;
                    int iloscBonusZarzadu = Convert.ToInt32(ConnectToSQL(connection_string, iloscBonusZarzaduQuery));


                    string iloscZyskOddzialuQuery = "SELECT count(*) from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp " +
                                                            " join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                             " WHERE Atk_id=268   and " + gidNumery + " and TrN_GIDTyp = " + doki[0].GIDTyp;
                    int iloscZyskOddzialu = Convert.ToInt32(ConnectToSQL(connection_string, iloscZyskOddzialuQuery));







                    /*SqlCommand typQuery = new SqlCommand("SELECT TOP (1) AtK_Nazwa from cdn.TraNag join cdn.TraElem on TrN_GIDNumer=TrE_GIDNumer and TrN_GIDTyp=Tre_GIDTyp" +
                                                            " join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                                                            " WHERE(AtK_Nazwa = 'Bonus zarządu' or Atk_Nazwa = 'Zysk handlowy oddziału' or Atk_Nazwa = 'Transport' or Atk_Nazwa = 'Palety') and TrN_GIDNumer = @GidNumer and TrN_GIDTyp = @GidTyp", connection);
                    typQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                    typQuery.Parameters["@GidNumer"].Value = gidNumer;
                    typQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                    typQuery.Parameters["@GidTyp"].Value = gidTyp;
                    string typ = typQuery.ExecuteScalar().ToString();*/

                    List<int> numery = new List<int>();
                    foreach (ListaDok dok in doki)
                    {

                        if (!numery.Contains(dok.GIDNumer))
                        {
                            XLZwiazanyDokInfo_20221 zwiazany = new XLZwiazanyDokInfo_20221();
                            zwiazany.Wersja = XLWersja;
                            zwiazany.GIDTyp = dok.GIDTyp;
                            zwiazany.GIDNumer = dok.GIDNumer;
                            int blad2 = cdn_api.cdn_api.XLDokumentyZwiazane(zwiazany);
                            if (blad2 != 0)
                            {
                                MessageBox.Show("Błąd dodawania dokumentu związanego: " + blad2, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            numery.Add(dok.GIDNumer);
                        }
                    }
                    numery.Clear();
                    //int l = 0;


                    if (czyTransport == true || czyPalety == true || czyPaletyZl == true)
                    {
                        if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                        {
                            if (iloscBonusZarzadu == iloscZyskOddzialu && (bonusWartosc >= 0 && oddzialWartosc >= 0))
                            {
                                if (iloscBonusZarzadu == ilosc)
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                else
                                {
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                }
                            }
                            else
                            {
                                if (iloscBonusZarzadu == ilosc && bonusWartosc >= 0)
                                {
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                }
                                else if (iloscZyskOddzialu == ilosc && oddzialWartosc >= 0)
                                {
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                }
                                else
                                {
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                }
                            }
                        }
                        else
                        {
                            if (czyBonusZarzadu == true)
                            {
                                if (iloscBonusZarzadu == ilosc && bonusWartosc >= 0)
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                else
                                {
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                }
                            }
                            else if (czyZyskOddzialu == true)
                            {
                                if (iloscZyskOddzialu == ilosc && oddzialWartosc >= 0)
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                else
                                {
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, false, false, false, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                    DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                                }

                            }
                            else
                                DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                        }

                    }
                    else if (czyBonusZarzadu == true && czyZyskOddzialu == true)
                    {
                        if (iloscBonusZarzadu == iloscZyskOddzialu && ((bonusWartosc >= 0 && oddzialWartosc >= 0) || (bonusWartosc < 0 && oddzialWartosc < 0)))
                            DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                        else
                        {
                            DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, false, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                            DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, false, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                        }
                    }
                    else
                    {
                        DodDok.dodajDokumentMalySpinacz(sesja, XLWersja, akronim, numerDok, czyBonusZarzadu, czyZyskOddzialu, czyTransport, czyPalety, czyPaletyZl, connection_string, 0, (int)gidNumer, (int)gidTyp, ref l, seria, doki, gidNumery, frs);
                    }






                }
                else if (czyCenaZero == "TAK")
                {
                    MessageBox.Show("W fakturze występują ceny 0zł. Anulowano dodawanie KDZ ", "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(" Błąd Wykonaj Mały KDZ: "+e.Message, "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        public static void UpdateUjemne(XLDokumentNagInfo_20221 dok,int lp, decimal wartosc, SqlConnection connection)
        {
            //string commandUpdateKDD = "update cdn.KosztyDodDok set KDD_Wartosc=" + wartosc.ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_GIDLp=" + lp;
            //SqlCommand updateCommandKDD = new SqlCommand(commandUpdateKDD, connection);
            //updateCommandKDD.ExecuteNonQuery();
            // string commandUpdate = "update cdn.TraSElem " +
            //                        " set TrS_KosztKsiegowy=(" + wartosc.ToString().Replace(",", ".") + "/(SELECT MAX(TrS_GIDLp) FROM cdn.TraSElem where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + ")), " +
            //                       " TrS_KosztRzeczywisty=(" + wartosc.ToString().Replace(",", ".") + "/(SELECT MAX(TrS_GIDLp) FROM cdn.TraSElem where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + ")) where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + " and TrS_SubGIDLp=" + lp;
            string commandUpdate = "update cdn.TraSElem " +
                                   " set TrS_KosztKsiegowy=(" + wartosc.ToString().Replace(",", ".") + "/(SELECT COUNT(*) FROM cdn.TraSElem where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + " and TrS_SubGidLp=1)), " +
                                  " TrS_KosztRzeczywisty=(" + wartosc.ToString().Replace(",", ".") + "/(SELECT count(*) FROM cdn.TraSElem where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + " and TrS_SubGidLp=1)) where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + " and TrS_SubGIDLp=" + lp;

           SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
            updateCommand.ExecuteNonQuery();



        }

        public static void UpdateUjemneAll(XLDokumentNagInfo_20221 dok,  SqlConnection connection, string connection_string)
        {
            //string commandUpdateKDD = "update cdn.KosztyDodDok set KDD_Wartosc=" + wartosc.ToString().Replace(",", ".") + " where KDD_GIDNumer=" + dok.GIDNumer + "  and KDD_GIDTyp =" + dok.GIDTyp + " and KDD_GIDLp=" + lp;
            //SqlCommand updateCommandKDD = new SqlCommand(commandUpdateKDD, connection);
            //updateCommandKDD.ExecuteNonQuery();
            // string commandUpdate = "update cdn.TraSElem " +
            //                        " set TrS_KosztKsiegowy=(" + wartosc.ToString().Replace(",", ".") + "/(SELECT MAX(TrS_GIDLp) FROM cdn.TraSElem where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + ")), " +
            //                       " TrS_KosztRzeczywisty=(" + wartosc.ToString().Replace(",", ".") + "/(SELECT MAX(TrS_GIDLp) FROM cdn.TraSElem where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + ")) where TrS_GIDNumer=" + dok.GIDNumer + "  and TrS_GIDTyp =" + dok.GIDTyp + " and TrS_SubGIDLp=" + lp;

            string commandSum = "SELECT SUM(KDD_Wartosc) FROM cdn.KosztyDodDok where KDD_GIDTyp=" + dok.GIDTyp + " AND KDD_GIDNumer=" + dok.GIDNumer;
            SqlCommand Sum = new SqlCommand(commandSum, connection);
            string suma = Sum.ExecuteScalar().ToString();

            string commandUpdate = "Update s set TrS_KosztKsiegowy = ROUND((TrE_KosztKsiegowy / TrE_Ilosc * TrS_Ilosc) * KDD_Wartosc / "+suma.Replace(",",".")+", 2), " +
                                    " TrS_KosztRzeczywisty = ROUND((TrE_KosztKsiegowy / TrE_Ilosc * TrS_Ilosc) * KDD_Wartosc / " + suma.Replace(",", ".") + ", 2) FROM cdn.TraSElem as s " +
                                    " join cdn.TRaElem on TrS_GIDTyp = TrE_GIdTyp and TrS_GIDNumer = TrE_GIDNumer and TrS_GiDLp = TrE_GIDLp " +
                                     "join cdn.KosztyDodDok on KDD_GIDNumer = TrS_GIDNumer and KDD_GIDTyp = TRS_GIDTyp and KDD_GIDLp = TrS_SubGIDLp where (TrE_GIDTyp = "+dok.GIDTyp+" AND TrE_GIDNumer = "+dok.GIDNumer+")";
            SqlCommand updateCommand = new SqlCommand(commandUpdate, connection);
            updateCommand.ExecuteNonQuery();

            string commandCzyRoznica = "SELECT SUM(TRS_KosztKsiegowy) from cdn.TraSElem where (TrS_GIDTyp=" + dok.GIDTyp + " AND TrS_GIDNumer=" + dok.GIDNumer + ") and ABS(TrS_KosztKsiegowy)< 1000000000";
            SqlCommand czyRoznicaCommand = new SqlCommand(commandCzyRoznica, connection);
            string czyRoznica = czyRoznicaCommand.ExecuteScalar().ToString();

            if(Convert.ToDouble(czyRoznica)!=Convert.ToDouble(suma))
            {
                double roznica = Convert.ToDouble(suma) - Convert.ToDouble(czyRoznica);
                commandUpdate = "Update cdn.TraSElem set TrS_KosztKsiegowy = "+roznica.ToString().Replace(",",".")+", " +
                                    " TrS_KosztRzeczywisty = " + roznica.ToString().Replace(",", ".") + 
                                    " where (TrS_GIDTyp = " + dok.GIDTyp + " AND TrS_GIDNumer = " + dok.GIDNumer + " and ABS(TrS_KosztKsiegowy)> 1000000000)";
                updateCommand = new SqlCommand(commandUpdate, connection);
                updateCommand.ExecuteNonQuery();

            }

            string selectLp = "SELECT TrS_GIDLp from cdn.TraSElem where (TrS_GIDTyp=" + dok.GIDTyp + " AND TrS_GIDNumer=" + dok.GIDNumer + ")";
            List<int> lpy = SelectListRowColumn(selectLp, "cdn.TraSElem", connection_string);

            foreach (int lp in lpy)
            {

                string selectSubLp = "SELECT TrS_SubGIDLp from cdn.TraSElem where (TrS_GIDTyp=" + dok.GIDTyp + " AND TrS_GIDNumer=" + dok.GIDNumer + " and TrS_GIDLp="+lp+")";
                List<int> subLpy = SelectListRowColumn(selectSubLp, "cdn.TraSElem", connection_string);

                foreach (int subLp in subLpy)
                {
                    string dstDane = "SELECT 'Kwota:'+ STRING_AGG(i.TrS_KosztKsiegowy, '; ')+ 'Ilość: '+String_AGG(i.TrS_Ilosc,'; ')+ 'Dokumenty: '+ " +
                        " string_Agg(cdn.NumerDokumentu(t.TrN_GIDTyp, t.TrN_SpiTyp, t.TrN_TrNTyp, t.TrN_TrNNumer, t.TrN_TrNRok, t.TrN_TrNSeria, t.TrN_TrNMiesiac), '; ')" +
                        " FROM cdn.TraSElem as i join cdn.TwrZasoby as z  on z.TwZ_DstNumer = i.TrS_DstNumer and z.TwZ_DstTyp = i.TrS_DstTyp " +
                        " join cdn.TraSElem as e on z.TwZ_DstNumer = e.TrS_DstNumer and z.TwZ_DstTyp = e.TrS_DstTyp" +
                        " join cdn.TraNag as t on t.trN_GIDNumer = i.TrS_GIDNumer and t.TRN_GIDTYp = i.TrS_GIDTyp " +
                        " where(e.TrS_GIDTyp = " + dok.GIDTyp + " AND e.TrS_GIDNumer = " + dok.GIDNumer + "  and e.TrS_GIDLp = " + lp + " and e.TrS_SubGIDLp = " + subLp + ") ";

                    SqlCommand commandZasoby = new SqlCommand(dstDane, connection);

                    string dstKsiegowa = "SELECT TwZ_KsiegowaNetto = (SELECT SUM(TrS_KosztKsiegowy) FROM cdn.TraSElem wHERE (TrS_DstNumer=z.TwZ_DstNumer and TrS_DstTyp=z.TwZ_DstTyp ))" +
                        " FROM cdn.TwrZasoby as z join cdn.TraSElem as i on z.TwZ_DstNumer = i.TrS_DstNumer and z.TwZ_DstTyp = i.TrS_DstTyp"
                        + " where (TrS_GIDTyp=" + dok.GIDTyp + " AND TrS_GIDNumer=" + dok.GIDNumer + " and TrS_GIDLp=" + lp + " and TrS_SubGIDLp=" + subLp + ")" ;
                    SqlCommand commandKsiegowa = new SqlCommand(dstKsiegowa, connection);

                    //MessageBox.Show(commandZasoby.ExecuteScalar().ToString()+ "\ndla dokumentu: (TrS_GIDTyp=" + dok.GIDTyp + " AND TrS_GIDNumer=" + dok.GIDNumer + " and TrS_GIDLp=" + lp + " and TrS_SubGIDLp=" + subLp + ")"+
                     //   "\nWartosc księgowa w TwrZasoby: "+ commandKsiegowa.ExecuteScalar().ToString());

                    string commandUpdateZasoby = "UPDATE z  set TwZ_KsiegowaNetto = (SELECT SUM(TrS_KosztKsiegowy) FROM cdn.TraSElem wHERE (TrS_DstNumer=z.TwZ_DstNumer and TrS_DstTyp=z.TwZ_DstTyp)), "
                                                + " TwZ_RzeczywistaNetto = (SELECT SUM(TrS_KosztRzeczywisty) FROM cdn.TraSElem wHERE (TrS_DstNumer=z.TwZ_DstNumer and TrS_DstTyp=z.TwZ_DstTyp)) "
                                                + " FROM cdn.TwrZasoby as z join cdn.TraSElem as i on z.TwZ_DstNumer = i.TrS_DstNumer and z.TwZ_DstTyp = i.TrS_DstTyp"
                                                + " where (TrS_GIDTyp=" + dok.GIDTyp + " AND TrS_GIDNumer=" + dok.GIDNumer + " and TrS_GIDLp=" + lp + " and TrS_SubGIDLp="+subLp+")";
                    SqlCommand updateCommandZasoby = new SqlCommand(commandUpdateZasoby, connection);
                    updateCommandZasoby.ExecuteNonQuery();
                }
            }



        }

        /* public static void SprawdzZasoby(int gidNumer, List<int>KDZty, SqlConnection connection, string connection_string)
         {
             string towaryQuery = "SELECT Tre_TwrNumer from cdn.TraElem WHERE(TrE_GIDTyp= 1521 AND TrE_GIDNumer = "+gidNumer+")";
             List<int> towary = SelectListRowColumn(towaryQuery, "cdn.TraEleme", connection_string);

             foreach(int twrNumer in towary)
             {
                 string dstNumerQuery = "if exists (SELECT * FROM cdn.TwrZasoby WHERE TwZ_TwrNumer = " + twrNumer + " AND TwZ_KsiegowaNetto> 1000000) SELECT TwZ_DstNumer FROM cdn.TwrZasoby " +
                                     " WHERE TwZ_TwrNumer = " + twrNumer + " AND TwZ_KsiegowaNetto> 1000000 else SELECT 0";
                 SqlCommand sql = new SqlCommand(dstNumerQuery, connection);
                 int dstNumer = Convert.ToInt32(sql.ExecuteReader().ToString());
                 if(dstNumer!=0)
                 {

                 }

             }

         }*/



        public static decimal WyliczZarzad(SqlConnection connection, int gidNumer, int gidTyp)
        {
            try
            {
                int data = 0;
                SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                               " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                if (data == 0)
                {
                    SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                    data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                    data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                    data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());
                    // data = dok.Data;
                }

                SqlCommand selectZarzadWartosc = new SqlCommand(" SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                           + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                           + " WHERE( Atk_id=281 or Atk_id=265) and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data ", connection);
                selectZarzadWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
                selectZarzadWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
                selectZarzadWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
                selectZarzadWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
                selectZarzadWartosc.Parameters.Add("@data", SqlDbType.Int);
                selectZarzadWartosc.Parameters["@data"].Value = data;
                return Convert.ToDecimal(selectZarzadWartosc.ExecuteScalar().ToString());

            }
            catch (Exception)
            {
                return 0;
            }


        }

        public static decimal WyliczOddzial(SqlConnection connection , int gidNumer, int gidTyp)
        {
            try { 
            int data = 0;
            SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                           " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
            dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
            dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
            dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
            dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
            data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

            if (data == 0)
            {
                SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());
                // data = dok.Data;
            }

            SqlCommand selectOddzialWartosc = new SqlCommand(" SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                       + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                       + " WHERE( Atk_id=268) and TrE_GIDTyp = @GidTyp AND TrE_GIDNumer = @GidNumer and AtH_TimeStampDo>= @data  and ath_TimeStamp<=@data ", connection);
            selectOddzialWartosc.Parameters.Add("@GidNumer", SqlDbType.Int);
            selectOddzialWartosc.Parameters["@GidNumer"].Value = (int)gidNumer;
            selectOddzialWartosc.Parameters.Add("@GidTyp", SqlDbType.Int);
            selectOddzialWartosc.Parameters["@GidTyp"].Value = (int)gidTyp;
            selectOddzialWartosc.Parameters.Add("@data", SqlDbType.Int);
            selectOddzialWartosc.Parameters["@data"].Value = data;
            return Convert.ToDecimal(selectOddzialWartosc.ExecuteScalar().ToString());
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static decimal WyliczTransport(SqlConnection connection, int gidTyp, int gidNumer)
        {
            string command= "SELECt SUM(ROUND(Atr_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer=Atr_ObiNumer and TrE_TwrTyp=Atr_ObiTyp  join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID " +
                        " WHERE Atk_id=267 and TrE_GIDTyp = " + gidTyp + " AND TrE_GIDNumer = " + gidNumer;

            SqlCommand selectTransportWartosc = new SqlCommand(command, connection);
            return Convert.ToDecimal(selectTransportWartosc.ExecuteScalar().ToString());
            

        }


        public static decimal WyliczOddzialMalySpinacz(SqlConnection connection, List<ListaDok> doki, string numeryPZ, int gidTyp, int gidNumer)
        {
            try
            {


                int data = 0;
                SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                               " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                if (data == 0)
                {
                    SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                    data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                    data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                    data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());
                    // data = dok.Data;
                }

                string command = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                         + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                         + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                         + " WHERE Atk_id=268 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi") + " and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                         + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                         + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                         + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                         + " WHERE Atk_id=268 and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi") + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                SqlCommand selectZyskWartosc = new SqlCommand(command, connection);
                return Convert.ToDecimal(selectZyskWartosc.ExecuteScalar().ToString());
            } 
            catch (Exception)
            {
                return 0;
            }
}

        public static decimal WyliczBonusMalySpinacz(SqlConnection connection, List<ListaDok> doki, string numeryPZ, int gidTyp, int gidNumer)
        {
            try
            {
                int data = 0;
                SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                               " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                if (data == 0)
                {
                    SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                    data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                    data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                    data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());
                    // data = dok.Data;
                }

                string command = " if exists(SELECT * FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                         + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                         + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                         + " WHERE ( Atk_id=281 or Atk_id=265) and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi") + " and AtH_TimeStampDo >= " + data + "  and  ath_TimeStamp <= " + data + ") "
                                                                         + " SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie,2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                         + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                         + " join cdn.TraSElem on TrS_GIDNumer=TrE_GIDNumer and TrS_GIDTyp=TrE_GIDTyp and TrS_GIDLp=TrE_GIDLp AND TrS_SubGIDLp=1"
                                                                         + " WHERE ( Atk_id=281 or Atk_id=265) and TrS_SpiTyp = " + doki[0].GIDTyp + " AND " + numeryPZ.Replace("TrE_GID", "TrS_Spi") + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data + " else SELECt 0";

                SqlCommand selectBonusWartosc = new SqlCommand(command, connection);
                return Convert.ToDecimal(selectBonusWartosc.ExecuteScalar().ToString());
            }
            catch (Exception)
            {
                return 0;
            }

        }


        public static decimal WyliczBonusSpinacz(SqlConnection connection, List<ListaDok> doki, string gidNumery, int gidTyp, int gidNumer)
        {
            try
            {
                int data = 0;
                SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                               " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                if (data == 0)
                {
                    SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                    data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                    data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                    data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());
                    // data = dok.Data;
                }

                string command = "SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie, 2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                              + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                              + " WHERE (Atk_id=265 or Atk_id=281)  and  TrE_GIDNumer  IN (" + gidNumery + ") and TrE_GIDTyp = " + doki[0].GIDTyp + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data ;



                SqlCommand selectBonusWartosc = new SqlCommand(command, connection);
                return Convert.ToDecimal(selectBonusWartosc.ExecuteScalar().ToString());
            }
            catch (Exception)
            {
                return 0;
            }


        }


        public static decimal WyliczOddzialSpinacz(SqlConnection connection, List<ListaDok> doki, string gidNumery, int gidTyp, int gidNumer)
        {
            try
            {
                int data = 0;
                SqlCommand dataQuery = new SqlCommand("if ((SELECT TrN_ZaNNumer from cdn.TraNag WHERE (TrN_GIDTyp=@GidTyp AND TrN_GIDNumer=@GidNumer)) !=0 ) SELECT ZaN_DataWystawienia FROM cdn.ZamNag " +
                                                               " join cdn.TraNag on TrN_ZaNNumer = ZaN_GIDNumer and TrN_ZaNTyp = ZaN_GIDTyp WHERE(TrN_GIDTyp = @GidTyp AND TrN_GIDNumer = @GidNumer) else SELECT '0'", connection);
                dataQuery.Parameters.Add("@GidNumer", SqlDbType.Int);
                dataQuery.Parameters["@GidNumer"].Value = (int)gidNumer;
                dataQuery.Parameters.Add("@GidTyp", SqlDbType.Int);
                dataQuery.Parameters["@GidTyp"].Value = (int)gidTyp;
                data = Convert.ToInt32(dataQuery.ExecuteScalar().ToString());

                if (data == 0)
                {
                    SqlCommand data2Query = new SqlCommand("SELECT DateDiff(day, DateAdd(day, -4, '1801-01-01'), @SqlDateTime)", connection);
                    data2Query.Parameters.Add("@SqlDateTime", SqlDbType.DateTime);
                    data2Query.Parameters["@SqlDateTime"].Value = DateTime.Today;
                    data = Convert.ToInt32(data2Query.ExecuteScalar().ToString());
                    // data = dok.Data;
                }

                string command = "SELECt SUM(ROUND(AtH_Wartosc / 100.0 * TrE_WartoscPoRabacie, 2)) FROM cdn.TraElem join cdn.Atrybuty on TrE_TwrNumer = Atr_ObiNumer and TrE_TwrTyp = Atr_ObiTyp "
                                                                              + " join cdn.AtrybutyKlasy on Atr_AtkId = Atk_ID join cdn.AtrybutyHist on Atr_AtkId = AtH_AtkId and Tre_TwrTyp = AtH_ObiTyp and Tre_TwrNumer = AtH_ObiNumer "
                                                                              + " WHERE Atk_id=268  and  TrE_GIDNumer  IN (" + gidNumery + ") and TrE_GIDTyp = " + doki[0].GIDTyp + " and AtH_TimeStampDo>= " + data + "  and ath_TimeStamp<=" + data;



                SqlCommand selectBonusWartosc = new SqlCommand(command, connection);
                return Convert.ToDecimal(selectBonusWartosc.ExecuteScalar().ToString());
            }
            catch (Exception )
            {
                //MessageBox.Show("Błąd: "+e.Message)
                return 0;
            }


        }


        public static  List<int> SelectListRowColumn(string query, string srcTableName, string connection_string)
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
        }



        public static string ConnectToSQL(string connection_string, string query)
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

        /*
        public static string Update(string sql, string connection_string)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    connection.Open();
                    adapter.UpdateCommand = new SqlCommand(sql, connection);
                    adapter.UpdateCommand.ExecuteNonQuery();
                    //MessageBox.Show("Row updated !! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return sql;
        }*/

       /* public static string DeleteFrom(string sql, string connection_string)       //Użycie delete i inserta nie jest optymalne, ale w niektórych przypadkach pozycje nie miały atrybutu. Po testach zmienić na update
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    connection.Open();
                    adapter.DeleteCommand = new SqlCommand(sql, connection);
                    adapter.DeleteCommand.ExecuteNonQuery();
                    //MessageBox.Show("Row deleted!! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Komunikat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return sql;
        }
       */
    }
}
