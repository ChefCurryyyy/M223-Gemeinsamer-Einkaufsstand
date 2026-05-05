import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ItemDto } from '../../shared/models/models';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private base = `${environment.apiUrl}/shoppinglists`;

  constructor(private http: HttpClient) {}

  create(listId: number, name: string, amount: number, unit: string) {
    return this.http.post<ItemDto>(`${this.base}/${listId}/items`, { name, amount, unit });
  }

  update(listId: number, itemId: number, name: string, amount: number, unit: string) {
    return this.http.put<ItemDto>(`${this.base}/${listId}/items/${itemId}`, { name, amount, unit });
  }

  toggleBought(listId: number, itemId: number, isBought: boolean) {
    return this.http.patch<ItemDto>(`${this.base}/${listId}/items/${itemId}/bought`, { isBought });
  }

  delete(listId: number, itemId: number) {
    return this.http.delete(`${this.base}/${listId}/items/${itemId}`);
  }
}
