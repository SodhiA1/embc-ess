import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { combineLatest, of, Observable } from 'rxjs';
import { switchMap, tap } from 'rxjs/operators';

import { AuthService } from '../core/services/auth.service';
import { OrganizationService } from '../core/services/organization.service';
import { VolunteerService, VolunteerSearchQueryParameters } from '../core/services/volunteer.service';
import { ListResult, Volunteer, PaginationSummary, User, Organization } from '../core/models';

@Component({
  selector: 'app-volunteer-list',
  templateUrl: './volunteer-list.component.html',
  styleUrls: ['./volunteer-list.component.scss']
})
export class VolunteerListComponent implements OnInit {
  // simple server response
  resultsAndPagination: ListResult<Volunteer>;
  notFoundMessage = 'Searching ...';

  // who's accessing this list component
  currentUser: User;
  isLocalAuthority: boolean;
  isProvincialAdmin: boolean;

  // the parent organization of all volunteers shown in this list
  currentOrganization: Organization | null = null;

  // local constants used in the FORM
  readonly SHOW_ALL = 1;
  readonly SHOW_ADMINS_ONLY = 2;
  readonly SHOW_ESS_USERS_ONLY = 3;

  // collection of pagination parameters for UI pagination
  // display and pagination
  increments: number[] = [5, 10, 25, 50, 100, 1000];
  // doesn't need to be an object besides it provides a visual seper
  page: number; // the current displayed page
  totalPages: number; // how many pages are returned?
  pageSize: number; // how many entries are on the page
  previousQuery: string; // a place to save the last query parameters
  sort: string = ''; // how do we sort the list
  collectionSize: number = 0; // how large is the collection?
  maxSize = 10; // how many pages of results shoudl the UI show before collapsing?
  boundaryLinks = true; // do we show the jump to first and last page links?

  // the search form and associated toggles (show all, show only admins, show only regular users)
  form: FormGroup;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private volunteers: VolunteerService,
    private organizations: OrganizationService,
    private auth: AuthService,
  ) { }

  // convenience getters
  get results(): Volunteer[] {
    // if there are non null results in the global variable to contain the volunteers return the collection of data otherwise return null
    return this.resultsAndPagination ? this.resultsAndPagination.data : null;
  }

  get pagination(): PaginationSummary {
    // if there are non null results in the global pagination summary to contain the results and pagination return the metadata otherwise return null
    return this.resultsAndPagination ? this.resultsAndPagination.metadata : null;
  }

  ngOnInit() {
    // initialize the form component
    this.initSearchForm();
    // initialize the global variables. When done get volunteers associated with the organization
    this.initVars()
      .subscribe(org => this.getVolunteers({ org_id: org ? org.id : null }));
  }

  initSearchForm(): void {
    // instantiate the form builder group
    this.form = this.fb.group({
      // clear the search box
      searchbox: null,
      // set the user toggle to a constant integer that defines the view for the user
      userToggle: this.SHOW_ALL,
    });
  }

  initVars(): Observable<Organization> {
    // combine everything we need to display this form into one Observable
    const combined$ = combineLatest(
      this.auth.getCurrentUser(),
      this.auth.isLocalAuthority$,
      this.auth.isProvincialAdmin$,
      this.route.queryParamMap,
    );

    // subscribe to the single observable, giving us all the data we need
    return combined$
      .pipe(
        tap(([user, isLocalAuthority, isProvincialAdmin]) => {
          this.currentUser = user;
          this.isLocalAuthority = isLocalAuthority;
          this.isProvincialAdmin = isProvincialAdmin;
        }),
        switchMap(([user, isLocalAuthority, isProvincialAdmin, queryParams]) => {
          // get the organization matching the query parameter passed in (if any)
          if (isProvincialAdmin) {
            const orgId = queryParams.get('orgId');
            return this.getAnyOrganization(orgId);
          }
          // for local authorities, we know what org they belong to
          if (isLocalAuthority) {
            const volunteerId = user.contactid;
            return this.getVolunteerOrganization(volunteerId);
          }
        }),
        tap(org => this.currentOrganization = org)
      );
  }

  getAnyOrganization(orgId: string): Observable<Organization> {
    // get an organization by its ID
    if (!orgId) {
      // if there is no organization ID included return null
      return of(null as Organization);
    }
    // if there is an organization id included look up the organization in the service by its ID
    return this.organizations.getOrganizationById(orgId);
  }

  getVolunteerOrganization(volunteerId: string): Observable<Organization> {
    // get the organization associated with the volunteer
    if (!volunteerId) {
      // if there is no volunteer ID included return null because a null volunteer cannot have an organization
      return of(null as Organization);
    }
    // get a volunteer by their ID and collect their organization by its ID
    return this.volunteers
      .getVolunteerById(volunteerId)
      .pipe(switchMap(v => this.organizations.getOrganizationById(v.organization.id)));
  }

  // get volunteers with supplied params defaults defined in
  getVolunteers(params: VolunteerSearchQueryParameters = {}) {
    // collect pagination parameters and add them into the supplied params

    // go get the collection of meta and data
    // get the volunteers using the parameters supplied
    this.volunteers.getVolunteers(params)
      .subscribe((v: ListResult<Volunteer>) => {
        // save the result of the service into an object with both the result and service
        this.resultsAndPagination = v;
        // collect all of the meta into variables
        this.page = v.metadata.page;
        this.totalPages = v.metadata.totalPages;
        this.collectionSize = v.metadata.totalCount;
        this.maxSize = v.metadata.pageSize;
        //save the last query performed
        this.previousQuery = params.q || '';

        // Set the not found result message. It should be hidden when results flow into the form
        this.notFoundMessage = 'No results found.';
      });
  }

  onPageChange(page: number = this.page) {
    // change the page that we want
    this.page = page;
    // search again on whatever the last query was (or blank)
    this.getVolunteers(this.createQueryParams());
  }
  // submit and collect search
  search() {
    if (!this.currentOrganization || !this.currentOrganization.id) {
      // do not get all volunteers in the system
      return;
    }
    this.getVolunteers(this.createQueryParams());
  }

  createQueryParams(): VolunteerSearchQueryParameters {
    // helper function that collects the form values
    // the check if the numeric value of the toggle matches the global constant for view
    const essOnly = this.form.value.userToggle == this.SHOW_ESS_USERS_ONLY;
    const adminOnly = this.form.value.userToggle == this.SHOW_ADMINS_ONLY;
    return {
      q: this.form.value.searchbox,
      org_id: this.currentOrganization.id,
      ess_only: essOnly,
      admin_only: adminOnly,
      offset: this.page * this.maxSize, // pagination
      limit: this.maxSize, // pagination
    };
  }
}
