import React, { useRef, useEffect, useState } from 'react';
import { useGameStore } from '../store/gameStore';

interface DrawingCanvasProps {
  isEnabled: boolean;
}

export const DrawingCanvas: React.FC<DrawingCanvasProps> = ({ isEnabled }) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const contextRef = useRef<CanvasRenderingContext2D | null>(null);
  const { currentDrawing, setCurrentDrawing } = useGameStore();

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    canvas.width = 500;
    canvas.height = 500;
    
    const context = canvas.getContext('2d');
    if (!context) return;
    
    context.lineCap = 'round';
    context.strokeStyle = 'black';
    context.lineWidth = 2;
    contextRef.current = context;

    // Clear canvas
    context.fillStyle = 'white';
    context.fillRect(0, 0, canvas.width, canvas.height);
    
    // Restore drawing if it exists
    if (currentDrawing) {
      const img = new Image();
      img.onload = () => {
        context.drawImage(img, 0, 0);
      };
      img.src = currentDrawing;
    }
  }, [currentDrawing]);

  const startDrawing = (event: React.MouseEvent) => {
    if (!isEnabled) return;
    const { offsetX, offsetY } = event.nativeEvent;
    contextRef.current?.beginPath();
    contextRef.current?.moveTo(offsetX, offsetY);
    setIsDrawing(true);
  };

  const draw = (event: React.MouseEvent) => {
    if (!isDrawing || !isEnabled) return;
    const { offsetX, offsetY } = event.nativeEvent;
    contextRef.current?.lineTo(offsetX, offsetY);
    contextRef.current?.stroke();
  };

  const stopDrawing = () => {
    if (!isEnabled) return;
    contextRef.current?.closePath();
    setIsDrawing(false);
    // Save the drawing data when finished
    if (canvasRef.current) {
      setCurrentDrawing(canvasRef.current.toDataURL());
    }
  };

  return (
    <canvas
      ref={canvasRef}
      className={`border-2 ${isEnabled ? 'border-blue-500' : 'border-gray-300'} rounded-lg bg-white`}
      onMouseDown={startDrawing}
      onMouseMove={draw}
      onMouseUp={stopDrawing}
      onMouseLeave={stopDrawing}
    />
  );
};