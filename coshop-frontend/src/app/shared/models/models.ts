// ── Auth ──────────────────────────────────────────────────────────────────────
export interface AuthResponse {
  token: string;
  userId: number;
  username: string;
  role: string;          // 'User' | 'Admin' — neu vom Backend
}

// ── User ──────────────────────────────────────────────────────────────────────
export interface UserDto {
  id: number;
  username: string;
  email: string;
  role: string;
}

// ── Shopping List ─────────────────────────────────────────────────────────────
export interface ShoppingListSummary {
  id: number;
  title: string;
  createdAt: string;
  ownerId: number;
  itemCount: number;
  memberCount: number;
}

export interface ShoppingListDetail {
  id: number;
  title: string;
  createdAt: string;
  ownerId: number;
  ownerUsername: string;
  items: ItemDto[];
  members: MemberDto[];
}

// ── Item ──────────────────────────────────────────────────────────────────────
export interface ItemDto {
  id: number;
  name: string;
  amount: number;
  unit: string;
  isBought: boolean;
  listId: number;
  lastModifiedByUserId: number;
  lastModifiedByUsername: string;
}

// ── Member ────────────────────────────────────────────────────────────────────
export interface MemberDto {
  userId: number;
  username: string;
  joinedAt: string;
}