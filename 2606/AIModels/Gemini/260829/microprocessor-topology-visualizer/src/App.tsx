/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { useState, useEffect } from 'react';
import defaultManifest from './manifest.json';
import FlowGraph from './FlowGraph';

export default function App() {
  const [manifestText, setManifestText] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setManifestText(JSON.stringify(defaultManifest, null, 2));
  }, []);

  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newVal = e.target.value;
    setManifestText(newVal);
    
    try {
      JSON.parse(newVal);
      setError(null);
    } catch (err) {
      setError("Invalid JSON format");
    }
  };

  return (
    <div className="flex flex-col md:flex-row h-screen bg-slate-900 text-slate-100 font-sans overflow-hidden">
      {/* Sidebar: JSON Editor */}
      <div className="w-full md:w-1/3 h-1/2 md:h-full border-b md:border-b-0 md:border-r border-slate-700 flex flex-col bg-slate-900">
        <div className="p-4 bg-slate-800 border-b border-slate-700 flex justify-between items-center">
          <h2 className="text-lg font-bold text-slate-200">Manifest Editor</h2>
          {error && <span className="text-xs text-red-400 bg-red-400/10 px-2 py-1 rounded">{error}</span>}
        </div>
        <textarea
          value={manifestText}
          onChange={handleTextChange}
          className="flex-1 w-full bg-slate-900 text-green-400 p-4 font-mono text-sm resize-none focus:outline-none focus:ring-1 focus:ring-indigo-500"
          spellCheck="false"
        />
      </div>

      {/* Main Canvas: React Flow */}
      <div className="w-full md:w-2/3 h-1/2 md:h-full relative bg-slate-950 flex flex-col">
        <div className="absolute top-4 left-4 z-10 bg-slate-800/80 backdrop-blur-sm border border-slate-700 px-4 py-2 rounded-lg shadow-lg pointer-events-none">
          <h1 className="text-xl font-bold text-slate-100">Microprocessor Topology</h1>
          <div className="flex gap-4 mt-2 text-xs text-slate-400">
            <span className="flex items-center gap-1"><div className="w-2 h-2 rounded-full bg-indigo-500"></div> Publishes</span>
            <span className="flex items-center gap-1"><div className="w-2 h-2 rounded-full bg-emerald-500"></div> Subscribes</span>
          </div>
        </div>
        <div className="flex-1">
          {manifestText && !error ? (
            <FlowGraph manifestJson={manifestText} />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-slate-500">
              {error ? 'Fix JSON errors to render graph' : 'Loading...'}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
