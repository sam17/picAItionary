import { useState, useEffect } from 'react';
import { useGameStore } from '../store/gameStore';

interface TimerProps {
  duration: number;
  onTimeUp: () => void;
}

export const Timer: React.FC<TimerProps> = ({ duration, onTimeUp }) => {
  const [timeLeft, setTimeLeft] = useState(duration);
  const { currentModifierType } = useGameStore();

  // Adjust duration based on speed round modifier
  useEffect(() => {
    if (currentModifierType === 'speed') {
      setTimeLeft(Math.floor(duration / 2));
    } else {
      setTimeLeft(duration);
    }
  }, [duration, currentModifierType]);

  useEffect(() => {
    const interval = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          onTimeUp();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [onTimeUp]);

  return (
    <div className="text-lg sm:text-2xl font-bold flex items-center gap-2">
      <span className="text-gray-600">Time:</span>
      <span className={`tabular-nums ${currentModifierType === 'speed' ? 'text-red-500' : ''}`}>
        {timeLeft}s
      </span>
    </div>
  );
};