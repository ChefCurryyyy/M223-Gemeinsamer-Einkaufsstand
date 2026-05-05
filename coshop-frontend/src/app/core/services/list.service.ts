import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ShoppingListDetail, ShoppingListSummary, MemberDto } from '../../shared/models/models';

@Injectable({ providedIn: 'root' })
export class ListService {
  private base = `${environment.apiUrl}/shoppinglists`;

  constructor(private http: HttpClient) {}

  getAll()               { return this.http.get<ShoppingListSummary[]>(this.base); }
  getById(id: number)    { return this.http.get<ShoppingListDetail>(`${this.base}/${id}`); }
  create(title: string)  { return this.http.post<ShoppingListSummary>(this.base, { title }); }
  update(id: number, title: string) { return this.http.put(`${this.base}/${id}`, { title }); }
  delete(id: number)     { return this.http.delete(`${this.base}/${id}`); }

  inviteMember(listId: number, username: string) {
    return this.http.post<MemberDto>(`${this.base}/${listId}/members`, { username });
  }
  removeMember(listId: number, memberId: number) {
    return this.http.delete(`${this.base}/${listId}/members/${memberId}`);
  }
}
