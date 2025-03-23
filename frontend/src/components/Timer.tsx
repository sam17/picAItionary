import React, { useState, useEffect } from 'react';

interface TimerProps {
  duration: number;
  onTimeUp: () => void;
}

export const Timer: React.FC<TimerProps> = ({ duration, onTimeUp }) => {
  const [timeLeft, setTimeLeft] = useState(duration);

  useEffect(() => {
    setTimeLeft(duration);
    
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
  }, [duration, onTimeUp]);

  return (
    <div className="text-2xl font-bold">
      Time: {timeLeft}s
    </div>
  );
};