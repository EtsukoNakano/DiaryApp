using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;

namespace DiaryApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            using (var conn = new SQLiteConnection("Data Source=DiaryDb.sqlite"))
            {
                //データベースに接続
                conn.Open();
                //コマンドの実行
                using (var command = conn.CreateCommand())
                {
                    //テーブルが存在しなければ作成する
                    ///<summary>
                    ///テーブル名：diary　/　フィールド：text(※本文)　date(※日付)
                    ///</summary>
                    /*StringBuilder sb = new StringBuilder();
                    sb.Append("CREATE TABLE IF NOT EXISTS diary(");
                    sb.Append("text TEXT NOT NULL");
                    sb.Append(" , date TEXT NOT NULL");
                    sb.Append(")");*/

                    ///command.CommandText = sb.ToString();
                    command.CommandText = "CREATE TABLE IF NOT EXISTS diary( text TEXT NOT NULL, date TEXT NOT NULL)";
                    command.ExecuteNonQuery();
                }
                //切断
                conn.Close();

                //日記一覧を更新
                DiaryListUpdateToDataGrid();
            }
        }

        //日記一覧を更新するメソッド(DB更新後に自動で呼び出し)
        public void DiaryListUpdateToDataGrid()
        {
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=DiaryDb.sqlite"))
            {
                //データベースに接続
                conn.Open();
                //日記一覧の表示
                using (DataSet dataSet = new DataSet())
                {
                    //データを取得(1000行分の一覧を日付の新しい順で表示している)
                    String sql = "SELECT date, text From diary ORDER BY date DESC LIMIT 1000";
                    SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, conn);
                    dataAdapter.Fill(dataSet);
                    //データグリッドに表示
                    this.DiaryListViewGrid.AutoGenerateColumns = false;//自動で列を作成しない(列が重複して表示されるため)
                    this.DiaryListViewGrid.DataContext = dataSet.Tables[0].DefaultView;
                }
                //切断
                conn.Close();
            }
        }

        //日記登録ボタンのイベントハンドラ
        private void AddDiaryBtnClick(object sender, RoutedEventArgs e)
        {
            AddDiaryToDb();
        }
        //日記登録のためのメソッド
        private void AddDiaryToDb()
        {
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=DiaryDb.sqlite"))
            {
                //データベースに接続
                conn.Open();
                //日記データの登録
                using (DataSet dataSet = new DataSet())
                {
                    if (this.textBoxForAddDiary.Text != "") //TextBoxが空でなければ登録
                    {
                        String sql = String.Format("INSERT INTO diary (date , text) VALUES ('{0}', '{1}')", DateTime.Now, this.textBoxForAddDiary.Text);
                        SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, conn);
                        dataAdapter.Fill(dataSet);
                        MessageBox.Show("日記を新規登録しました。", caption: "登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
                        // -->caption:は名前付き引数での指定だが、全引数をを順番に指定しているので不要
                        this.textBoxForAddDiary.Text = "";
                    }
                    else
                    {
                        MessageBox.Show("日記の本文が入力されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                conn.Close(); //DBを切断
                DiaryListUpdateToDataGrid();//一覧の表示を更新
            }
        }

        //削除ボタンのイベントハンドラと処理
        private void RsetBtnClick(object sender, RoutedEventArgs e)
        {
            this.textBoxForAddDiary.Text = "";
        }

        //本文を表示ボタンのイベントハンドラ
        private void ShowDiaryBtnClick(object sender, RoutedEventArgs e)
        {
            //ここにはdateで抽出したtextをtextboxForAddDiaryに表示させる
            ShowDiaryToDb();
        }
        //日記一覧で選択した行の本文をTextBoxに表示
        private void ShowDiaryToDb()
        {
            var dgSI = this.DiaryListViewGrid.SelectedItem;
            int rowIdx = this.DiaryListViewGrid.Items.IndexOf(dgSI);//選択行インデックスを取得
            if (rowIdx != -1) //選択されてなければ-1なので
            {
                //選択行の本文を文字列としてcellValueに代入してTextBoxに表示
                DataRowView dataRow = (DataRowView)dgSI;
                string cellValue = dataRow.Row.ItemArray[1].ToString(); //選択行の本文を取得
                this.textBoxForAddDiary.Text += cellValue;
            }
            else
            {
                MessageBox.Show("日記一覧が選択されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //選択行を削除ボタンのイベントハンドラ
        private void DeleteDiaryBtnClick(object sender, RoutedEventArgs e)
        {
            DeleteDiaryToDb();
        }
        //選択行の日記をDBから削除
        private void DeleteDiaryToDb()
        {
            var dgSI = this.DiaryListViewGrid.SelectedItem;
            int rowIdx = this.DiaryListViewGrid.Items.IndexOf(dgSI);//選択行インデックスを取得
            if (rowIdx != -1)//一覧選択がなければ-1なので
            {
                using (SQLiteConnection conn = new SQLiteConnection("Data Source=DiaryDb.sqlite"))
                using (var command = conn.CreateCommand())
                {
                    conn.Open(); //DBに接続

                    DataRowView dataRow = (DataRowView)dgSI;
                    string date = dataRow.Row.ItemArray[0].ToString(); //選択行の日時を取得

                    //SQLコマンドを生成(コマンド文への変数埋め込みにS"{...}"は使えない)
                    command.CommandText = @"DELETE FROM diary WHERE date=@date"; //@~で変数を埋め込む場所を指定
                    command.Parameters.Add(new SQLiteParameter("@date", date)); //変数dateを@dateに埋め込む

                    //削除の最終確認
                    var confirmation = MessageBox.Show("選択した日記を削除しますか？", "最終確認", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (confirmation == MessageBoxResult.OK) //メッセージボックスでOKを選択していたら
                    {
                        command.ExecuteNonQuery(); //SQLを実行してDBから選択行を削除
                    }
                    conn.Close(); //DBを切断
                    DiaryListUpdateToDataGrid(); //一覧の表示を更新
                }
            }
            else
            {
                MessageBox.Show("日記一覧が選択されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //日記を修正ボタンのイベントハンドラ
        private void FixDiaryBtnClick(object sender, RoutedEventArgs e)
        {
            FixDiaryToDB();
        }

        private void FixDiaryToDB()
        {
            string text = this.textBoxForAddDiary.Text; //テキストボックスの文字列を取得
            if (text != "")
            {
                var dgSI = this.DiaryListViewGrid.SelectedItem;
                int rowIdx = this.DiaryListViewGrid.Items.IndexOf(dgSI);//選択行インデックスを取得
                if (rowIdx != -1)
                {
                    using (SQLiteConnection conn = new SQLiteConnection("Data Source=DiaryDb.sqlite"))
                    using (var command = conn.CreateCommand())
                    {
                        conn.Open(); //DBに接続
                        
                        //選択行のdateを文字列として取得
                        DataRowView dataRow = (DataRowView)dgSI;
                        string date = dataRow.Row.ItemArray[0].ToString();

                        //SQLコマンドを生成
                        command.CommandText = @"UPDATE diary SET text=@text WHERE date=@date";
                        command.Parameters.Add(new SQLiteParameter("@text", $"{text}")); //文字列の代入はこの記述でないとオブジェクト参照できない
                        command.Parameters.Add(new SQLiteParameter("@date", date));

                        //修正の最終確認
                        //削除の最終確認
                        var confirmation = MessageBox.Show("下記の内容で日記を修正しますか？", "最終確認", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        if (confirmation == MessageBoxResult.OK) //メッセージボックスでOKを選択していたら
                        {
                            command.ExecuteNonQuery(); //SQLを実行して選択行のテキストを修正
                            this.textBoxForAddDiary.Text = "";
                        }
                        conn.Close(); //DBを切断
                        DiaryListUpdateToDataGrid(); //一覧の表示を更新
                    }
                }
                else
                {
                    MessageBox.Show("日記一覧が選択されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }else
            {
                MessageBox.Show("日記の本文が入力されていません。", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}