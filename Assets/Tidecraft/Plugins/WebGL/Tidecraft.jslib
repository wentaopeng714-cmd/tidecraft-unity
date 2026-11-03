mergeInto(LibraryManager.library, {
  Tidecraft_ReportState: function(walls, towers, connected, ready) {
    var canvas = document.getElementById('unity-canvas');
    if (!canvas) return;
    canvas.dataset.wallCount = String(walls);
    canvas.dataset.towerCount = String(towers);
    canvas.dataset.basinWet = connected ? 'true' : 'false';
    canvas.dataset.gameReady = ready ? 'true' : 'false';
  },
  Tidecraft_ReportWaves: function(phase, wave, health, hits, broken, flooded, paused) {
    var canvas = document.getElementById('unity-canvas');
    if (!canvas) return;
    canvas.dataset.wavePhase = UTF8ToString(phase);
    canvas.dataset.wave = String(wave);
    canvas.dataset.goalHealth = String(health);
    canvas.dataset.defenceHits = String(hits);
    canvas.dataset.brokenDefences = String(broken);
    canvas.dataset.surgeCells = String(flooded);
    canvas.dataset.wavesPaused = paused ? 'true' : 'false';
  }
});
