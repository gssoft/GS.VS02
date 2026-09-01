import React, { useEffect, useMemo } from 'react';
import {
  ReactFlow,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  Node,
  Edge,
  MarkerType,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { getLayoutedElements } from './layout';

interface FlowGraphProps {
  manifestJson: string;
}

interface Manifest {
  processors: Array<{
    name: string;
    subscribesTo: string[];
    publishes: string[];
  }>;
}

export default function FlowGraph({ manifestJson }: FlowGraphProps) {
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);

  useEffect(() => {
    try {
      const manifest: Manifest = JSON.parse(manifestJson);
      
      const newNodes: Node[] = [];
      const newEdges: Edge[] = [];
      const eventSet = new Set<string>();

      manifest.processors.forEach(p => {
        // Add Processor Node
        newNodes.push({
          id: p.name,
          type: 'default',
          data: { label: p.name },
          className: 'bg-slate-800 text-slate-100 border-2 border-slate-600 rounded-lg p-4 font-bold shadow-lg w-[250px] text-center',
        });

        // Collect Events
        p.publishes.forEach(evt => eventSet.add(evt));
        p.subscribesTo.forEach(evt => eventSet.add(evt));
      });

      // Add Event Nodes
      eventSet.forEach(evt => {
        newNodes.push({
          id: evt,
          type: 'default',
          data: { label: evt },
          className: 'bg-indigo-900/40 text-indigo-200 border-2 border-indigo-500/50 rounded-full p-2 text-sm shadow-md w-[150px] text-center',
        });
      });

      // Add Edges
      manifest.processors.forEach(p => {
        p.publishes.forEach(evt => {
          newEdges.push({
            id: `pub-${p.name}-${evt}`,
            source: p.name,
            target: evt,
            animated: true,
            style: { stroke: '#6366f1', strokeWidth: 2 }, // indigo
            markerEnd: { type: MarkerType.ArrowClosed, color: '#6366f1' },
          });
        });

        p.subscribesTo.forEach(evt => {
          newEdges.push({
            id: `sub-${evt}-${p.name}`,
            source: evt,
            target: p.name,
            animated: true,
            style: { stroke: '#10b981', strokeWidth: 2 }, // emerald
            markerEnd: { type: MarkerType.ArrowClosed, color: '#10b981' },
          });
        });
      });

      // Apply Layout
      const { nodes: layoutedNodes, edges: layoutedEdges } = getLayoutedElements(
        newNodes,
        newEdges,
        'TB'
      );

      setNodes(layoutedNodes);
      setEdges(layoutedEdges);

    } catch (err) {
      // JSON parse error or similar - do nothing, keep previous state
    }
  }, [manifestJson, setNodes, setEdges]);

  return (
    <div className="w-full h-full bg-slate-950">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        fitView
        colorMode="dark"
        minZoom={0.2}
      >
        <Background color="#334155" gap={16} />
        <Controls className="bg-slate-800 border-slate-700 fill-slate-200" />
      </ReactFlow>
    </div>
  );
}
