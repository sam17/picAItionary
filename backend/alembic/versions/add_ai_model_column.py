"""add ai_model column to game_rounds

Revision ID: add_ai_model
Revises: add_ai_explanation
Create Date: 2024-04-12 12:20:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision: str = 'add_ai_model'
down_revision: Union[str, None] = 'add_ai_explanation'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    # Add ai_model column to game_rounds table
    op.add_column('game_rounds', sa.Column('ai_model', sa.String(), nullable=True))
    
    # Update existing rows to use gpt-4o-mini
    op.execute("UPDATE game_rounds SET ai_model = 'gpt-4o-mini'")


def downgrade() -> None:
    # Remove ai_model column from game_rounds table
    op.drop_column('game_rounds', 'ai_model') 