"""add ai_explanation column to game_rounds

Revision ID: add_ai_explanation
Revises: 87c5a0d742dd
Create Date: 2024-04-12 12:15:00.000000

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision: str = 'add_ai_explanation'
down_revision: Union[str, None] = '87c5a0d742dd'
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    # Add ai_explanation column to game_rounds table
    op.add_column('game_rounds', sa.Column('ai_explanation', sa.String(), nullable=True))


def downgrade() -> None:
    # Remove ai_explanation column from game_rounds table
    op.drop_column('game_rounds', 'ai_explanation') 