import { useRef, useEffect, useState, useCallback } from 'react';
import { useGameStore } from '../store/gameStore';
import { Eraser, Pencil } from 'lucide-react';

interface DrawingCanvasProps {
  isEnabled: boolean;
}

type DrawingEvent = MouseEvent | TouchEvent;

export const DrawingCanvas: React.FC<DrawingCanvasProps> = ({ isEnabled }) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [isEraserMode, setIsEraserMode] = useState(false);
  const [thickness, setThickness] = useState(2);
  const contextRef = useRef<CanvasRenderingContext2D | null>(null);
  const { currentDrawing, setCurrentDrawing } = useGameStore();

  const resizeCanvas = useCallback(() => {
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
    context.strokeStyle = isEraserMode ? 'white' : 'black';
    context.lineWidth = Math.max(thickness, size / 250) * (isEraserMode ? 2 : 1);
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
  }, [currentDrawing, isEraserMode, thickness]);

  useEffect(() => {
    resizeCanvas();
    
    // Add resize event listener
    window.addEventListener('resize', resizeCanvas);
    
    // Cleanup
    return () => {
      window.removeEventListener('resize', resizeCanvas);
    };
  }, [resizeCanvas]);

  const getCoordinates = useCallback((event: DrawingEvent) => {
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
      x: event.offsetX,
      y: event.offsetY
    };
  }, []);

  const startDrawing = useCallback((event: DrawingEvent) => {
    if (!isEnabled) return;
    event.preventDefault();
    const { x, y } = getCoordinates(event);
    const context = contextRef.current;
    if (!context) return;

    context.beginPath();
    context.moveTo(x, y);
    context.strokeStyle = isEraserMode ? 'white' : 'black';
    context.lineWidth = Math.max(thickness, canvasRef.current?.width ? canvasRef.current.width / 250 : 2) * (isEraserMode ? 2 : 1);
    setIsDrawing(true);
  }, [isEnabled, getCoordinates, isEraserMode, thickness]);

  const draw = useCallback((event: DrawingEvent) => {
    if (!isDrawing || !isEnabled) return;
    event.preventDefault();
    const { x, y } = getCoordinates(event);
    contextRef.current?.lineTo(x, y);
    contextRef.current?.stroke();
  }, [isDrawing, isEnabled, getCoordinates]);

  const stopDrawing = useCallback(() => {
    if (!isEnabled) return;
    contextRef.current?.closePath();
    setIsDrawing(false);
    
    // Save the current drawing
    const canvas = canvasRef.current;
    if (canvas) {
      setCurrentDrawing(canvas.toDataURL());
    }
  }, [isEnabled, setCurrentDrawing]);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    // Add touch event listeners with passive: false
    canvas.addEventListener('touchstart', startDrawing, { passive: false });
    canvas.addEventListener('touchmove', draw, { passive: false });
    canvas.addEventListener('touchend', stopDrawing);
    canvas.addEventListener('touchcancel', stopDrawing);

    // Add mouse event listeners
    canvas.addEventListener('mousedown', startDrawing);
    canvas.addEventListener('mousemove', draw);
    canvas.addEventListener('mouseup', stopDrawing);
    canvas.addEventListener('mouseout', stopDrawing);

    return () => {
      // Remove touch event listeners
      canvas.removeEventListener('touchstart', startDrawing);
      canvas.removeEventListener('touchmove', draw);
      canvas.removeEventListener('touchend', stopDrawing);
      canvas.removeEventListener('touchcancel', stopDrawing);

      // Remove mouse event listeners
      canvas.removeEventListener('mousedown', startDrawing);
      canvas.removeEventListener('mousemove', draw);
      canvas.removeEventListener('mouseup', stopDrawing);
      canvas.removeEventListener('mouseout', stopDrawing);
    };
  }, [startDrawing, draw, stopDrawing]);

  return (
    <div className="w-full flex flex-col items-center gap-4">
      <div className="flex flex-col sm:flex-row items-center gap-4 w-full max-w-[500px]">
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setIsEraserMode(false)}
            className={`p-2 rounded-md ${!isEraserMode ? 'bg-blue-100' : 'bg-gray-100'}`}
            disabled={!isEnabled}
          >
            <Pencil className="w-5 h-5" />
          </button>
          <button
            type="button"
            onClick={() => setIsEraserMode(true)}
            className={`p-2 rounded-md ${isEraserMode ? 'bg-blue-100' : 'bg-gray-100'}`}
            disabled={!isEnabled}
          >
            <Eraser className="w-5 h-5" />
          </button>
        </div>
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <span className="text-sm whitespace-nowrap">Thickness:</span>
          <input
            type="range"
            min="1"
            max="20"
            value={thickness}
            onChange={(e) => setThickness(Number(e.target.value))}
            className="w-full sm:w-32"
            disabled={!isEnabled}
          />
          <span className="text-sm w-6 text-center">{thickness}</span>
        </div>
      </div>
      <canvas
        ref={canvasRef}
        className={`border-2 ${isEnabled ? 'border-blue-500' : 'border-gray-300'} rounded-lg bg-white touch-none max-w-[500px] w-full aspect-square`}
        style={{ touchAction: 'none' }}
      />
    </div>
  );
};