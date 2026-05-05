import { Injectable, inject, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ItemDto } from '../../shared/models/models';

// ── Event shapes (mirror C# HubEvents.cs) ────────────────────────────────────
export interface ItemCreatedEvent   { listId: number; itemId: number; name: string; amount: number; unit: string; actorUserId: number; actorUsername: string; }
export interface ItemUpdatedEvent   { listId: number; itemId: number; name: string; amount: number; unit: string; actorUserId: number; actorUsername: string; }
export interface ItemDeletedEvent   { listId: number; itemId: number; actorUserId: number; }
export interface ItemBoughtEvent    { listId: number; itemId: number; isBought: boolean; actorUserId: number; actorUsername: string; }
export interface ListRenamedEvent   { listId: number; newTitle: string; actorUserId: number; }
export interface ListDeletedEvent   { listId: number; actorUserId: number; }
export interface MemberAddedEvent   { listId: number; userId: number; username: string; }
export interface MemberRemovedEvent { listId: number; userId: number; }

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  private currentListId: number | null = null;

  // Typed event streams — components subscribe to exactly what they need
  itemCreated$    = new Subject<ItemCreatedEvent>();
  itemUpdated$    = new Subject<ItemUpdatedEvent>();
  itemDeleted$    = new Subject<ItemDeletedEvent>();
  itemBought$     = new Subject<ItemBoughtEvent>();
  listRenamed$    = new Subject<ListRenamedEvent>();
  listDeleted$    = new Subject<ListDeletedEvent>();
  memberAdded$    = new Subject<MemberAddedEvent>();
  memberRemoved$  = new Subject<MemberRemovedEvent>();
  connected$      = new Subject<boolean>();

  async connectToList(listId: number): Promise<void> {
    // Disconnect from previous list if any
    if (this.currentListId !== null && this.currentListId !== listId) {
      await this.leaveList(this.currentListId);
    }

    // Build connection if not yet established
    if (!this.connection) {
      this.connection = new signalR.HubConnectionBuilder()
          .withUrl(`${environment.hubUrl}`, {
            accessTokenFactory: () => this.auth.getToken() ?? '',
            transport: signalR.HttpTransportType.WebSockets,
          })
          .withAutomaticReconnect([0, 2000, 5000, 10000])
          .configureLogging(signalR.LogLevel.Warning)
          .build();

      this.registerHandlers();

      this.connection.onreconnected(() => {
        this.connected$.next(true);
        if (this.currentListId !== null) {
          this.connection!.invoke('JoinList', this.currentListId);
        }
      });

      this.connection.onclose(() => this.connected$.next(false));
    }

    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }

    await this.connection.invoke('JoinList', listId);
    this.currentListId = listId;
    this.connected$.next(true);
  }

  async leaveList(listId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveList', listId);
    }
    this.currentListId = null;
  }

  async disconnect(): Promise<void> {
    if (this.currentListId !== null) await this.leaveList(this.currentListId);
    await this.connection?.stop();
    this.connection = null;
    this.connected$.next(false);
  }

  private registerHandlers(): void {
    if (!this.connection) return;
    this.connection.on('ItemCreated',      e => this.itemCreated$.next(e));
    this.connection.on('ItemUpdated',      e => this.itemUpdated$.next(e));
    this.connection.on('ItemDeleted',      e => this.itemDeleted$.next(e));
    this.connection.on('ItemBoughtToggled',e => this.itemBought$.next(e));
    this.connection.on('ListRenamed',      e => this.listRenamed$.next(e));
    this.connection.on('ListDeleted',      e => this.listDeleted$.next(e));
    this.connection.on('MemberAdded',      e => this.memberAdded$.next(e));
    this.connection.on('MemberRemoved',    e => this.memberRemoved$.next(e));
  }

  ngOnDestroy(): void { this.disconnect(); }
}