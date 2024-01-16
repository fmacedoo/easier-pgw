import React from 'react';

function App() {
  async function go() {
    console.log('INDO');
    const response = await window.chrome.webview.hostObjects.gateway.operations();
    console.log({ response: JSON.parse(response) });
  }

  return (
    <div className="App">
      <header className="App-header">
        <p>
          Edit <code>src/App.tsx</code> and save to reload.
        </p>
        <button onClick={() => go()}>operations</button>
      </header>
    </div>
  );
}

export default App;
