namespace PremierLeagueCommittee;
using System.Text.Json;

public partial class MainPage : ContentPage
{
    private List<Team> _teams = new List<Team>();
    private List<DisciplinaryRecord> _decisions = new List<DisciplinaryRecord>();

    private bool _alternateTheme = false;

    // These three properties are used by the data binding in Section 6.
    private string _committeeStatus = "Load the original league table to begin.";
    public string CommitteeStatus
    {
        get { return _committeeStatus; }
        set
        {
            _committeeStatus = value;
            OnPropertyChanged();
        }
    }

    private int _totalAdjustment = 0;
    public int TotalAdjustment
    {
        get { return _totalAdjustment; }
        set
        {
            _totalAdjustment = value;
            OnPropertyChanged();
        }
    }

    private string _decisionLog = "No decisions yet.";
    public string DecisionLog
    {
        get { return _decisionLog; }
        set
        {
            _decisionLog = value;
            OnPropertyChanged();
        }
    }

    public MainPage()
    {
        InitializeComponent();

        // The colours in this lab assume a light theme. Without this line, a computer
        // set to dark mode would show white text on a light background.
        Application.Current!.UserAppTheme = AppTheme.Light;

        BindingContext = this;
    }

    private async void LoadOriginalTable_Clicked(object sender, EventArgs e)
    {
        //  Open premierleague.json from Resources/Raw.

        using Stream stream = await FileSystem.OpenAppPackageFileAsync("premierleague.json");
        using StreamReader reader = new StreamReader(stream);
        //  Read the JSON text from the file.
        string json = await reader.ReadToEndAsync();
        //  Deserialise the JSON into a List<Team> and store it in _teams.
        _teams = JsonSerializer.Deserialize<List<Team>>(json) ?? new List<Team>();
        CalculateTable();
        DisplayTable();

        CommitteeStatus = "Original league table loaded.";
    }

    private void AddDecision_Clicked(object sender, EventArgs e)
    {
        // : Read the three Entry controls.
        string clubName = (TeamNameEntry.Text ?? "").Trim();
        string pointsAdjustment = (PointsAdjustmentEntry.Text ?? "").Trim();
        string reason = (ReasonEntry.Text ?? "").Trim();
        // Check that a club name has been entered.

        if (clubName=="")
        {
            CommitteeStatus = "Please enter a club name ";
            return;
        }
        //  Check that the points adjustment is a valid integer.
        if(!int.TryParse(pointsAdjustment, out int adjustment)){
            CommitteeStatus = "Invalid number  ";
            return;
        }
        //  Check that a reason has been entered.
        if (reason=="")
        {
            CommitteeStatus = "Invalid Reason";
            return;
        }
        // Check that the club exists in the league table (use FindTeam).
       Team? team= FindTeam(clubName);
        if (team == null)
        {
            CommitteeStatus = "Table not updated!";
            return;
        }
        // Create a DisciplinaryRecord and add it to _decisions.
        DisciplinaryRecord record = new DisciplinaryRecord();
        record.TeamName = team.Name;
        record.PointsAdjustment = adjustment;
        record.Reason = reason;
        _decisions.Add(record);
        //  Recalculate and redisplay the table, and call UpdateDecisionSummary().
        CalculateTable();
        DisplayTable();
        UpdateDecisionSummary();
        // Update CommitteeStatus and clear the three Entry controls.
        CommitteeStatus = "Sucessufully";
        TeamNameEntry.Text = "";
        PointsAdjustmentEntry.Text = "";
        ReasonEntry.Text = "";
    }

    private async void SaveDecisions_Clicked(object sender, EventArgs e)
    {
        //  Create a CommitteeData object containing _decisions.
        CommitteeData data = new CommitteeData();
        data.Decisions = _decisions;
        //  Serialise it to JSON.
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        //  Write the JSON to decisions.json (use GetDecisionsPath()).
        string path = GetDecisionsPath();
        await File.WriteAllTextAsync(path, json);
        //  Update CommitteeStatus.
        CommitteeStatus = "Saved to " + path;
    }

    private async void LoadDecisions_Clicked(object sender, EventArgs e)
    {
        // TODO: Check whether decisions.json exists. If it doesn't, say so in
        //       CommitteeStatus and stop.

        // TODO: Read the JSON text from the file.

        // TODO: Deserialise it into a CommitteeData object.

        // TODO: Replace _decisions with the loaded decisions.

        // TODO: Recalculate and redisplay the table, and call UpdateDecisionSummary().

        // TODO: Update CommitteeStatus.
    }

    private void ChangeTheme_Clicked(object sender, EventArgs e)
    {
        _alternateTheme = !_alternateTheme;

        if (_alternateTheme)
        {
            Resources["PageBackground"] = Color.FromArgb("#FFF0E0");
        }
        else
        {
            Resources["PageBackground"] = Color.FromArgb("#F4F4F4");
        }
    }

    // ------------------------------------------------------------
    // The methods below are supplied. You do not need to change them.
    // ------------------------------------------------------------

    // Returns the team with the given name (ignoring upper and lower case),
    // or null if there is no such team.
    private Team? FindTeam(string name)
    {
        foreach (Team team in _teams)
        {
            if (string.Equals(team.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return team;
            }
        }

        return null;
    }

    private void ClearDecisions_Clicked(object sender, EventArgs e)
    {
        _decisions.Clear();

        CalculateTable();
        DisplayTable();
        UpdateDecisionSummary();

        CommitteeStatus = "All decisions cleared.";
    }

    private void CalculateTable()
    {
        // Reset adjusted points to the original points.
        foreach (Team team in _teams)
        {
            team.AdjustedPoints = team.Points;
        }

        // Apply every disciplinary decision.
        foreach (DisciplinaryRecord decision in _decisions)
        {
            foreach (Team team in _teams)
            {
                if (team.Name == decision.TeamName)
                {
                    team.AdjustedPoints += decision.PointsAdjustment;
                }
            }
        }

        // Reorder the table.
        _teams.Sort(CompareTeams);
    }

    // Comparison helper used by List.Sort().
    // Sort by adjusted points, then goal difference.
    private int CompareTeams(Team first, Team second)
    {
        int result = second.AdjustedPoints.CompareTo(first.AdjustedPoints);

        if (result == 0)
        {
            result = second.GoalDifference.CompareTo(first.GoalDifference);
        }

        return result;
    }

    private void DisplayTable()
    {
        LeagueTable.Children.Clear();
        LeagueTable.RowDefinitions.Clear();

        // Header row.
        LeagueTable.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        AddCell("Pos", 0, 0, true);
        AddCell("Club", 1, 0, true);
        AddCell("P", 2, 0, true);
        AddCell("W", 3, 0, true);
        AddCell("D", 4, 0, true);
        AddCell("L", 5, 0, true);
        AddCell("GD", 6, 0, true);
        AddCell("Pts", 7, 0, true);

        int position = 1;

        foreach (Team team in _teams)
        {
            int row = position;
            LeagueTable.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            AddCell(position.ToString(), 0, row);
            AddCell(team.Name, 1, row);
            AddCell(team.Played.ToString(), 2, row);
            AddCell(team.Won.ToString(), 3, row);
            AddCell(team.Drawn.ToString(), 4, row);
            AddCell(team.Lost.ToString(), 5, row);
            AddCell(team.GoalDifference.ToString(), 6, row);
            AddCell(team.AdjustedPoints.ToString(), 7, row);

            position++;
        }
    }

    private void AddCell(string text, int column, int row, bool isHeader = false)
    {
        Label label = new Label
        {
            Text = text,
            FontSize = 12,
            FontAttributes = isHeader ? FontAttributes.Bold : FontAttributes.None,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = column == 1 ? TextAlignment.Start : TextAlignment.Center
        };

        LeagueTable.Add(label, column, row);
    }

    // Works out the total adjustment and the list of decisions, and puts them
    // into the two bound properties. Call this whenever _decisions changes.
    private void UpdateDecisionSummary()
    {
        int total = 0;
        string log = "";

        foreach (DisciplinaryRecord decision in _decisions)
        {
            total += decision.PointsAdjustment;
            log += decision.TeamName + " (" + decision.PointsAdjustment + "): " + decision.Reason + "\n";
        }

        if (log == "")
        {
            log = "No decisions yet.";
        }

        TotalAdjustment = total;
        DecisionLog = log.TrimEnd();
    }

    // The full path of decisions.json in the application's data folder.
    private string GetDecisionsPath()
    {
        return Path.Combine(FileSystem.Current.AppDataDirectory, "decisions.json");
    }
}