using System;
using System.Reactive.Linq;

namespace ReactiveCollectionsTest
{
    public static class ObservableExtensions
    {
        public static IObservable<T> DoAfter<T>(
                this    IObservable<T>  source,
                        Action<T>       onNext)
            => Observable.Create<T>(observer => source.Subscribe(
                onNext:         value =>
                {
                    observer.OnNext(value);
                    onNext.Invoke(value);
                },
                onError:        observer.OnError,
                onCompleted:    observer.OnCompleted));

        public static IObservable<TOut> SelectSome<TIn, TOut>(
                this    IObservable<TIn>            source,
                        Action<TIn, Action<TOut>>   selector)
            => Observable.Create<TOut>(observer => source.Subscribe(
                onNext:         item => selector.Invoke(item, observer.OnNext),
                onError:        observer.OnError,
                onCompleted:    observer.OnCompleted));
    }
}
