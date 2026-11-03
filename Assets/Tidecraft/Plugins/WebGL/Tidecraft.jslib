mergeInto(LibraryManager.library, {
  Tidecraft_ReportState: function(walls, towers, connected, ready) {
    var canvas = document.getElementById('unity-canvas');
    if (!canvas) return;
    canvas.dataset.wallCount = String(walls);
    canvas.dataset.towerCount = String(towers);
    canvas.dataset.basinWet = connected ? 'true' : 'false';
    canvas.dataset.gameReady = ready ? 'true' : 'false';
  }
});
