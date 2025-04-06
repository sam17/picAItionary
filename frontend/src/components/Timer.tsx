import { useState, useEffect } from 'react';

interface TimerProps {
  duration: number;
  onTimeUp: () => void;
}

export const Timer: React.FC<TimerProps> = ({ duration, onTimeUp }) => {
  const [timeLeft, setTimeLeft] = useState(duration);

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
      <span className="tabular-nums">{timeLeft}s</span>
    </div>
  );
};