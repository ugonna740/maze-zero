mergeInto(LibraryManager.library, {
  MazeZeroRequestMode: function (modePtr) {
    var mode = UTF8ToString(modePtr);
    if (window.mazeZero && window.mazeZero.selectMode) window.mazeZero.selectMode(mode);
    else window.dispatchEvent(new CustomEvent('maze-zero-request-mode', { detail: { mode: mode } }));
  }
});
