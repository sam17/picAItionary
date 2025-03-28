import React, { useRef, useEffect, useState } from 'react';
import { useGameStore } from '../store/gameStore';

interface DrawingCanvasProps {
  isEnabled: boolean;
}

export const DrawingCanvas: React.FC<DrawingCanvasProps> = ({ isEnabled }) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const contextRef = useRef<CanvasRenderingContext2D | null>(null);
  const { currentDrawing, setCurrentDrawing, isDrawingPhase, setIsDrawingPhase, setAiGuess } = useGameStore();

  const resizeCanvas = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    // Get the container width (parent element)
    const container = canvas.parentElement;
    if (!container) return;

    // Calculate size based on container width (90% of container width)
    const size = Math.min(container.clientWidth * 0.9, 500);
    
    // Set canvas size
    canvas.width = size;
    canvas.height = size;

    // Restore context settings
    const context = canvas.getContext('2d');
    if (!context) return;
    
    context.lineCap = 'round';
    context.strokeStyle = 'black';
    context.lineWidth = Math.max(2, size / 250); // Adjust line width based on canvas size
    contextRef.current = context;

    // Clear canvas
    context.fillStyle = 'white';
    context.fillRect(0, 0, canvas.width, canvas.height);
    
    // Restore drawing if it exists
    if (currentDrawing) {
      const img = new Image();
      img.onload = () => {
        context.drawImage(img, 0, 0, canvas.width, canvas.height);
      };
      img.src = currentDrawing;
    }
  };

  useEffect(() => {
    resizeCanvas();
    
    // Add resize event listener
    window.addEventListener('resize', resizeCanvas);
    
    // Cleanup
    return () => {
      window.removeEventListener('resize', resizeCanvas);
    };
  }, [currentDrawing]);

  const getCoordinates = (event: React.MouseEvent | React.TouchEvent) => {
    if ('touches' in event) {
      const touch = event.touches[0];
      const rect = canvasRef.current?.getBoundingClientRect();
      if (!rect) return { x: 0, y: 0 };
      return {
        x: touch.clientX - rect.left,
        y: touch.clientY - rect.top
      };
    }
    return {
      x: event.nativeEvent.offsetX,
      y: event.nativeEvent.offsetY
    };
  };

  const startDrawing = (event: React.MouseEvent | React.TouchEvent) => {
    if (!isEnabled) return;
    event.preventDefault();
    const { x, y } = getCoordinates(event);
    contextRef.current?.beginPath();
    contextRef.current?.moveTo(x, y);
    setIsDrawing(true);
  };

  const draw = (event: React.MouseEvent | React.TouchEvent) => {
    if (!isDrawing || !isEnabled) return;
    event.preventDefault();
    const { x, y } = getCoordinates(event);
    contextRef.current?.lineTo(x, y);
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

  const handleDoneDrawing = async () => {
    if (!canvasRef.current || !isDrawingPhase) return;

    try {
      const imageData = canvasRef.current.toDataURL();
      const response = await fetch('http://localhost:8000/analyze-drawing', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          image_data: imageData,
          prompt: "What word is being drawn? Respond with just the word, nothing else."
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to analyze drawing');
      }

      const result = await response.json();
      console.log('Analysis result:', result);
      
      // Store the AI's guess
      setAiGuess(result.word);
      
      // Move to the next phase
      setIsDrawingPhase(false);
    } catch (error) {
      console.error('Error analyzing drawing:', error);
      // TODO: Show error to the user
    }
  };

  return (
    <div className="w-full flex flex-col items-center gap-4">
      <canvas
        ref={canvasRef}
        className={`border-2 ${isEnabled ? 'border-blue-500' : 'border-gray-300'} rounded-lg bg-white touch-none max-w-[500px] w-full aspect-square`}
        onMouseDown={startDrawing}
        onMouseMove={draw}
        onMouseUp={stopDrawing}
        onMouseLeave={stopDrawing}
        onTouchStart={startDrawing}
        onTouchMove={draw}
        onTouchEnd={stopDrawing}
      />
      {isDrawingPhase && (
        <button
          onClick={handleDoneDrawing}
          className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
        >
          Done Drawing
        </button>
      )}
    </div>
  );
};