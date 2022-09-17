using Content.Client.CrewManifest;
using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.LateJoin;

[GenerateTypedNameReferences]
public sealed partial class OuterRimLateJoinGui : FancyWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    private ClientGameTicker _gameTicker;
    private readonly NewVesselGui _vesselPurchaseUi = new();

    public OuterRimLateJoinGui()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        //ShipDescription.SetMessage("Select a ship.");
        _gameTicker = EntitySystem.Get<ClientGameTicker>();
        _gameTicker.LobbyJobsAvailableUpdated += UpdateUi;
        VesselSelection.VesselItemList.OnItemSelected += args =>
        {
            UpdateUi(_gameTicker.JobsAvailable);
        };
        CrewManifestButton.OnPressed += args =>
        {
            EntitySystem.Get<CrewManifestSystem>().RequestCrewManifest(_lastSelection);
        };

        PurchaseButton.OnPressed += args =>
        {
            _vesselPurchaseUi.OpenCenteredLeft();
        };

        UpdateUi(_gameTicker.JobsAvailable);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _gameTicker.LobbyJobsAvailableUpdated -= UpdateUi;
        _vesselPurchaseUi.Dispose();
    }

    private EntityUid _lastSelection = EntityUid.Invalid;

    private readonly Dictionary<string, OuterRimLateJoinJobButton> _buttons = new();

    public void UpdateUi(IReadOnlyDictionary<EntityUid, Dictionary<string, uint?>> obj)
    {
        PurchaseButton.Disabled = !_gameTicker.PurchaseAvailable;
        if (VesselSelection.Selected is null)
        {
            CrewManifestButton.Visible = false;
            return;
        }

        CrewManifestButton.Visible = true;

        var station = VesselSelection.Selected.Value;
        var jobs = obj[station];

        if (station != _lastSelection)
        {
            foreach (var (_, button) in _buttons)
            {
                JobList.RemoveChild(button);
            }
            _buttons.Clear();
        }

        _lastSelection = station;

        foreach (var (jobId, _) in jobs)
        {
            if (_buttons.ContainsKey(jobId))
                continue;

            var newButton = new OuterRimLateJoinJobButton(station, jobId, _gameTicker, _prototypeManager);
            newButton.OnPressed += args =>
            {
                Logger.InfoS("latejoin", $"Late joining as ID: {jobId}");
                _consoleHost.ExecuteCommand($"joingame {CommandParsing.Escape(jobId)} {station}");
                Close();
            };

            JobList.AddChild(newButton);

            _buttons.Add(jobId, newButton);
        }

    }
}
