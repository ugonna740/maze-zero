mergeInto(LibraryManager.library, {
  MazeZeroSocketConnect: function (urlPtr) {
    if (window.mazeZeroSocket) window.mazeZeroSocket.close();
    var socket = new WebSocket(UTF8ToString(urlPtr));
    window.mazeZeroSocket = socket;
    socket.onopen = function () { SendMessage('Multiplayer Client', 'OnSocketOpen', ''); };
    socket.onmessage = function (event) { SendMessage('Multiplayer Client', 'OnSocketMessage', event.data); };
    socket.onerror = function () { SendMessage('Multiplayer Client', 'OnSocketError', 'WebSocket connection failed.'); };
    socket.onclose = function () { SendMessage('Multiplayer Client', 'OnSocketClose', ''); };
  },
  MazeZeroSocketSend: function (jsonPtr) {
    var socket = window.mazeZeroSocket;
    if (socket && socket.readyState === WebSocket.OPEN) socket.send(UTF8ToString(jsonPtr));
  },
  MazeZeroSocketClose: function () {
    if (window.mazeZeroSocket) window.mazeZeroSocket.close();
    window.mazeZeroSocket = null;
  }
});
